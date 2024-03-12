namespace Trading.Bot.Services;

public class StreamProcessor : BackgroundService
{
    private readonly LiveTradeCache _liveTradeCache;
    private readonly ILogger<StreamProcessor> _logger;
    private readonly TradeConfiguration _tradeConfiguration;
    private readonly List<string> _instruments = new();
    private readonly Dictionary<string, DateTime> _lastCandleTimings = new();

    public StreamProcessor(LiveTradeCache liveTradeCache, ILogger<StreamProcessor> logger,
        TradeConfiguration tradeConfiguration)
    {
        _liveTradeCache = liveTradeCache;
        _logger = logger;
        _tradeConfiguration = tradeConfiguration;

        foreach (var tradeSetting in _tradeConfiguration.TradeSettings)
        {
            _lastCandleTimings[tradeSetting.Instrument] = DateTime.UtcNow.RoundDown(tradeSetting.CandleSpan);

            _instruments.Add(tradeSetting.Instrument);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var instrument in _instruments)
            {
                try
                {
                    if (_liveTradeCache.LivePrices.ContainsKey(instrument))
                    {
                        DetectNewCandle(_liveTradeCache.LivePrices[instrument]);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred when trying to detect a new candle");
                }
            }

            await Task.Delay(10, stoppingToken);
        }
    }

    private void DetectNewCandle(LivePrice livePrice)
    {
        var candleSpan = _tradeConfiguration.TradeSettings.First(x =>
            x.Instrument == livePrice.Instrument).CandleSpan;

        var current = livePrice.Time.RoundDown(candleSpan);

        if (current <= _lastCandleTimings[livePrice.Instrument]) return;

        _lastCandleTimings[livePrice.Instrument] = current;

        livePrice.Time = current;

        _liveTradeCache.AddToQueue(livePrice);
    }
}