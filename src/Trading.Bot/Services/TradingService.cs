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
        foreach (var settings in _tradeConfiguration.TradeSettings)
        {
            _lastCandleTimings[settings.Instrument] =
                await _apiService.GetLastCandleTime(settings.Instrument, settings.Granularity);

            _instruments.AddRange(await _apiService.GetInstruments(settings.Instrument));
        }

        while (!stoppingToken.IsCancellationRequested &&
               await _timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessCandles(await GetNewCandles());
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
                await _apiService.GetCandles(instrument, settings.Granularity, count: settings.MovingAverage + 1);

            if (!candles.Any()) return;

            var calcResult = candles.CalcBollingerBands(settings.MovingAverage, settings.StandardDeviation).Last();

            if (calcResult.Signal != Signal.None){}
                await TryPlaceTrade(settings, calcResult);
        });

        await Task.WhenAll(tasks);
    }

    private async Task TryPlaceTrade(TradeSettings settings, IndicatorBase indicator)
    {
        var openTrades = await _apiService.GetOpenTrades();

        if (openTrades.All(ot => ot.Instrument != settings.Instrument)) return;

        var instrument = _instruments.FirstOrDefault(i => i.Name == settings.Instrument);

        if (instrument is null) return;

        await _apiService.PlaceTrade(
            new Order(instrument, 100, indicator.Signal, indicator.StopLoss, indicator.TakeProfit));
    }
}