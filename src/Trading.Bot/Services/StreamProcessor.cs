namespace Trading.Bot.Services;

public class StreamProcessor : BackgroundService
{
    private readonly OandaStreamService _streamService;
    private readonly TradeConfiguration _tradeConfiguration;
    private readonly Dictionary<string, DateTime> _lastCandleTimings = new();
    public readonly Queue<LivePrice> LivePriceQueue = new();

    public StreamProcessor(OandaStreamService streamService, TradeConfiguration tradeConfiguration)
    {
        _streamService = streamService;
        _tradeConfiguration = tradeConfiguration;

        foreach (var tradeSetting in _tradeConfiguration.TradeSettings)
        {
            _lastCandleTimings[tradeSetting.Instrument] = DateTime.UtcNow.RoundDown(tradeSetting.CandleSpan.Minutes);
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Initialize(stoppingToken);

        var instruments = _tradeConfiguration.TradeSettings.Select(x => x.Instrument).ToArray();

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var instrument in instruments)
            {
                DetectNewCandle(_streamService.LivePrices[instrument]);
            }
        }

        return Task.CompletedTask;
    }

    private void DetectNewCandle(LivePrice livePrice)
    {
        var minutes = _tradeConfiguration.TradeSettings.First(x => 
            x.Instrument == livePrice.Instrument).CandleSpan.Minutes;

        var current = livePrice.Time.RoundDown(minutes);

        if (current > _lastCandleTimings[livePrice.Instrument])
        {
            _lastCandleTimings[livePrice.Instrument] = current;

            LivePriceQueue.Enqueue(livePrice);
        }
    }

    private void Initialize(CancellationToken stoppingToken)
    {
        var instruments = string.Join(',', _tradeConfiguration.TradeSettings.Select(x => x.Instrument));

        Task.Run(() => _streamService.StreamLivePrices(instruments), stoppingToken);
    }
}