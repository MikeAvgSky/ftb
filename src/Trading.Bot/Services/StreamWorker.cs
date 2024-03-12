namespace Trading.Bot.Services;

public class StreamWorker : BackgroundService
{
    private readonly OandaStreamService _streamService;
    private readonly string _instruments;

    public StreamWorker(OandaStreamService streamService, TradeConfiguration tradeConfiguration)
    {
        _streamService = streamService;
        _instruments = string.Join(',', tradeConfiguration.TradeSettings.Select(s => s.Instrument));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _streamService.StreamLivePrices(_instruments, stoppingToken);

            await Task.Delay(1000, stoppingToken);
        }
    }
}