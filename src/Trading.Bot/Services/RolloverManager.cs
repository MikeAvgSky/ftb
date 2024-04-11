namespace Trading.Bot.Services;

public class RolloverManager : BackgroundService
{
    private readonly OandaApiService _apiService;
    private readonly ILogger<RolloverManager> _logger;

    public RolloverManager(OandaApiService apiService, ILogger<RolloverManager> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Rollover Manager");

        var easternTime = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        using var timer = new CronTimer("59 16 * * 1-5", easternTime);

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