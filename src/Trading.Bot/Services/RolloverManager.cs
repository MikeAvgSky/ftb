namespace Trading.Bot.Services;

public class RolloverManager : BackgroundService
{
    private readonly ILogger<RolloverManager> _logger;
    private readonly OandaApiService _apiService;

    public RolloverManager(ILogger<RolloverManager> logger, OandaApiService apiService)
    {
        _logger = logger;
        _apiService = apiService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new CronTimer("59 20 * * 1-5", TimeZoneInfo.Utc);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var openTrades = await _apiService.GetOpenTrades();

                foreach (var trade in openTrades)
                {
                    await _apiService.CloseTrade(trade.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to close all open trades");
            }
        }
    }
}