namespace Trading.Bot.Services;

public class StopLossUpdater : BackgroundService
{
    private readonly ILogger<StopLossUpdater> _logger;
    private readonly OandaApiService _apiService;
    private readonly LiveTradeCache _liveTradeCache;
    private readonly ParallelOptions _options = new();

    public StopLossUpdater(ILogger<StopLossUpdater> logger, OandaApiService apiService,
        LiveTradeCache liveTradeCache, TradeConfiguration tradeConfiguration)
    {
        _logger = logger;
        _apiService = apiService;
        _liveTradeCache = liveTradeCache;
        _options.MaxDegreeOfParallelism = tradeConfiguration.TradeSettings.Length;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Parallel.ForEachAsync(_liveTradeCache.TrailingStopChannel.Reader.ReadAllAsync(stoppingToken),
                _options, async (trailingStop, token) =>
                {
                    try
                    {
                        await Task.Delay(1000, token);

                        await DetectStopLossUpdate(trailingStop, token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while trying to update a trade");
                    }
                });

            await Task.Delay(10, stoppingToken);
        }
    }

    private async Task DetectStopLossUpdate(TrailingStop trailingStop, CancellationToken token)
    {
        var trade = await _apiService.GetTrade(trailingStop.TradeId);

        if (trade is null || trade.State != "OPEN")
        {
            _logger.LogInformation("Trade {TradeId} not found or not open", trailingStop.TradeId);
            return;
        }

        if (trade.Price + trade.UnrealizedPL > trailingStop.StopLossTarget)
        {
            await TryUpdateStopLoss(trailingStop, trade, token);
        }
        else
        {
            _logger.LogInformation("Trade for {Instrument} is currently at {CurrentPrice}. Waiting to reach {Target}",
                trade.Instrument, trade.Price + trade.UnrealizedPL, trailingStop.StopLossTarget);

            await _liveTradeCache.TrailingStopChannel.Writer.WriteAsync(trailingStop, token);
        }
    }

    private async Task TryUpdateStopLoss(TrailingStop trailingStop, TradeResponse trade, CancellationToken token)
    {
        var update = new OrderUpdate(trailingStop.DisplayPrecision, trailingStop.StopLossTarget);

        var success = await _apiService.UpdateTrade(update, trade.Id);

        if (success)
        {
            var stopLoss = new TrailingStop
            {
                TradeId = trade.Id,
                StopLossTarget = GetNewTarget(trailingStop.StopLossTarget, trade.Price, trailingStop.RiskReward),
                RiskReward = trailingStop.RiskReward,
                DisplayPrecision = trailingStop.DisplayPrecision
            };

            await _liveTradeCache.TrailingStopChannel.Writer.WriteAsync(stopLoss, token);
        }
        else
        {
            _logger.LogWarning("Unable to update trade for {Instrument}", trade.Instrument);
        }
    }

    private static double GetNewTarget(double target, double current, double riskReward)
    {
        return target + (target - current) * riskReward;
    }
}