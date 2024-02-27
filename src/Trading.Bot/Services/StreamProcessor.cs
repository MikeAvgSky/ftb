namespace Trading.Bot.Services;

public class StreamProcessor : BackgroundService
{
    private readonly OandaStreamService _streamService;
    private readonly LivePriceCache _livePriceCache;
    private readonly ILogger<StreamProcessor> _logger;
    private readonly TradeConfiguration _tradeConfiguration;
    private readonly Dictionary<string, DateTime> _lastCandleTimings = new();

    public StreamProcessor(OandaStreamService streamService, LivePriceCache livePriceCache, ILogger<StreamProcessor> logger, TradeConfiguration tradeConfiguration)
    {
        _streamService = streamService;
        _livePriceCache = livePriceCache;
        _logger = logger;
        _tradeConfiguration = tradeConfiguration;

        foreach (var tradeSetting in _tradeConfiguration.TradeSettings)
        {
            _lastCandleTimings[tradeSetting.Instrument] = DateTime.UtcNow.RoundDown(tradeSetting.CandleSpan);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Initialize(stoppingToken);

        var instruments = _tradeConfiguration.TradeSettings.Select(x => x.Instrument).ToArray();

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var instrument in instruments)
            {
                try
                {
                    if (_livePriceCache.LivePrices.ContainsKey(instrument))
                    {
                        DetectNewCandle(_livePriceCache.LivePrices[instrument]);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred when trying to detect a new candle");
                }
            }

            await Task.Delay(250 / instruments.Length, stoppingToken);
        }
    }

    private void DetectNewCandle(LivePrice livePrice)
    {
        var candleSpan = _tradeConfiguration.TradeSettings.First(x => 
            x.Instrument == livePrice.Instrument).CandleSpan;

        var current = livePrice.Time.RoundDown(candleSpan);

        if (current <= _lastCandleTimings[livePrice.Instrument]) return;

        _lastCandleTimings[livePrice.Instrument] = current;

        _livePriceCache.AddToQueue(livePrice);
    }

    private void Initialize(CancellationToken stoppingToken)
    {
        var instruments = string.Join(',', 
            _tradeConfiguration.TradeSettings.Select(x => x.Instrument));

        Task.Run(() => _streamService.StreamLivePrices(instruments), stoppingToken);
    }
}