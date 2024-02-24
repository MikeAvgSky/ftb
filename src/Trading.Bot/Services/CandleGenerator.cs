namespace Trading.Bot.Services;

public class CandleGenerator : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            
        }

        return Task.CompletedTask;
    }
}