namespace Trading.Bot.Services;

public class TradeManager : BackgroundService
{
    private readonly OandaApiService _apiService;
    private readonly LivePriceCache _livePriceCache;
    private readonly TradeConfiguration _tradeConfiguration;
    private readonly List<Instrument> _instruments = new();

    public TradeManager(OandaApiService apiService, LivePriceCache livePriceCache, TradeConfiguration tradeConfiguration)
    {
        _apiService = apiService;
        _livePriceCache = livePriceCache;
        _tradeConfiguration = tradeConfiguration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Initialise();

        while (!stoppingToken.IsCancellationRequested)
        {
            while (_livePriceCache.LivePriceQueue.Count != 0)
            {
                if (!_livePriceCache.LivePriceQueue.TryDequeue(out var price)) continue;

                var settings = _tradeConfiguration.TradeSettings.First(x => x.Instrument == price.Instrument);

                if (!await NewCandleAvailable(settings, price, stoppingToken)) continue;

                var candles =
                    await _apiService.GetCandles(settings.Instrument, settings.Granularity, count: settings.MovingAverage * 2);

                if (!candles.Any()) return;

                var calcResult = candles.CalcBollingerBands(settings.MovingAverage, settings.StandardDeviation,
                    settings.MaxSpread, settings.MinGain, settings.RiskReward).Last();

                if (calcResult.Signal != Signal.None)
                {
                    await TryPlaceTrade(settings, calcResult);
                }
            }
        }
    }

    private async Task<bool> NewCandleAvailable(TradeSettings settings, LivePrice price, CancellationToken stoppingToken)
    {
        var retryCount = 0;

        Start:

        if (retryCount >= 10) return false;

        var currentTime = await _apiService.GetLastCandleTime(settings.Instrument, settings.Granularity);

        if (currentTime != default && currentTime == price.Time) return true;

        await Task.Delay(1000, stoppingToken);

        retryCount++;

        goto Start;
    }

    private async Task Initialise()
    {
        _instruments.AddRange(await _apiService.GetInstruments(string.Join(",",
            _tradeConfiguration.TradeSettings.Select(s => s.Instrument))));
    }

    private async Task TryPlaceTrade(TradeSettings settings, IndicatorBase indicator)
    {
        if (!await CanPlaceTrade(settings)) return;

        var instrument = _instruments.FirstOrDefault(i => i.Name == settings.Instrument);

        if (instrument is null) return;

        var tradeUnits = await GetTradeUnits(settings, indicator);

        await _apiService.PlaceTrade(
            new Order(instrument, tradeUnits, indicator.Signal, indicator.StopLoss, indicator.TakeProfit));
    }

    private async Task<double> GetTradeUnits(TradeSettings settings, IndicatorBase indicator)
    {
        var price = (await _apiService.GetPrices(settings.Instrument)).FirstOrDefault();

        if (price is null) return 0.0;

        var conversionFactor = indicator.Signal switch
        {
            Signal.Buy => price.HomeConversion,
            Signal.Sell => price.HomeConversion,
            _ => 1.0
        };

        var pipLocation = _instruments.FirstOrDefault(i => i.Name == settings.Instrument)?.PipLocation ?? 1.0;

        var numPips = indicator.Loss / pipLocation;

        var perPipLoss = _tradeConfiguration.TradeRisk / numPips;

        return perPipLoss / (conversionFactor * pipLocation);
    }

    private async Task<bool> CanPlaceTrade(TradeSettings settings)
    {
        var openTrades = await _apiService.GetOpenTrades();

        return openTrades.All(ot => ot.Instrument != settings.Instrument);
    }
}