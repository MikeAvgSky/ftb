namespace Trading.Bot.Services;

public class TradingService : BackgroundService
{
    private readonly OandaApiService _apiService;
    private readonly TradeConfiguration _tradeConfiguration;
    private readonly ConcurrentDictionary<string, DateTime> _lastCandleTimings = new();
    private readonly List<Instrument> _instruments = new();
    private readonly PeriodicTimer _timer;

    public TradingService(OandaApiService apiService, TradeConfiguration tradeConfiguration)
    {
        _apiService = apiService;
        _tradeConfiguration = tradeConfiguration;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(10.0));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Initialise();

        while (!stoppingToken.IsCancellationRequested &&
               await _timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessCandles(await GetNewCandles());
        }
    }

    private async Task Initialise()
    {
        _instruments.AddRange(await _apiService.GetInstruments(string.Join(",",
            _tradeConfiguration.TradeSettings.Select(s => s.Instrument))));

        foreach (var settings in _tradeConfiguration.TradeSettings)
        {
            _lastCandleTimings[settings.Instrument] =
                await _apiService.GetLastCandleTime(settings.Instrument, settings.Granularity);
        }
    }

    private async Task<string[]> GetNewCandles()
    {
        var tasks = _tradeConfiguration.TradeSettings.Select(async settings =>
        {
            var currentTime = await _apiService.GetLastCandleTime(settings.Instrument, settings.Granularity);

            if (currentTime == default) return string.Empty;

            if (currentTime <= _lastCandleTimings[settings.Instrument]) return string.Empty;

            _lastCandleTimings[settings.Instrument] = currentTime;

            return settings.Instrument;
        });

        var newCandles = await Task.WhenAll(tasks);

        return newCandles.Where(c => !string.IsNullOrEmpty(c)).ToArray();
    }

    private async Task ProcessCandles(IReadOnlyCollection<string> instruments)
    {
        if (!instruments.Any()) return;

        var tasks = instruments.Select(async instrument =>
        {
            var settings = _tradeConfiguration.TradeSettings.First(s => s.Instrument == instrument);

            var candles =
                await _apiService.GetCandles(instrument, settings.Granularity, count: settings.MovingAverage * 2);

            if (!candles.Any()) return;

            var calcResult = candles.CalcBollingerBands(settings.MovingAverage, settings.StandardDeviation,
                settings.MaxSpread, settings.MinGain, settings.RiskReward).Last();

            if (calcResult.Signal != Signal.None)
                await TryPlaceTrade(settings, calcResult);
        });

        await Task.WhenAll(tasks);
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
            Signal.Buy => price.QuoteHomeConversionFactors.PositiveUnits,
            Signal.Sell => price.QuoteHomeConversionFactors.NegativeUnits,
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