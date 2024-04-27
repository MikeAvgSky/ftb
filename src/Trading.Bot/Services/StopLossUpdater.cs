namespace Trading.Bot.Services;

public class StopLossUpdater : BackgroundService
{
    private readonly ILogger<StopLossUpdater> _logger;
    private readonly OandaApiService _apiService;
    private readonly LiveTradeCache _liveTradeCache;
    private readonly TradeConfiguration _tradeConfiguration;
    private readonly ParallelOptions _options = new();

    public StopLossUpdater(ILogger<StopLossUpdater> logger, OandaApiService apiService,
        LiveTradeCache liveTradeCache, TradeConfiguration tradeConfiguration)
    {
        _logger = logger;
        _apiService = apiService;
        _liveTradeCache = liveTradeCache;
        _tradeConfiguration = tradeConfiguration;
        _options.MaxDegreeOfParallelism = _tradeConfiguration.TradeSettings.Length;
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
                        var trade = await _apiService.GetTrade(trailingStop.TradeId);

                        if (trade is null || trade.State != "OPEN")
                        {
                            _logger.LogWarning("Trade {TradeId} not found or not open", trailingStop.TradeId);
                            return;
                        }

                        var tradeSettings =
                            _tradeConfiguration.TradeSettings.First(x => x.Instrument == trade.Instrument);

                        var profitTarget = GetProfitTarget(trade.Price, trailingStop.StopLoss, tradeSettings.RiskReward,
                            trailingStop.DisplayPrecision);

                        if (trade.Price + trade.UnrealizedPL >= profitTarget)
                        {
                            var update = new OrderUpdate(trailingStop.StopLoss);

                            var success = await _apiService.UpdateTrade(update, trade.Id);

                            if (success)
                            {
                                var stopLoss = new TrailingStop(trade.Id, profitTarget, trailingStop.DisplayPrecision);

                                await _liveTradeCache.TrailingStopChannel.Writer.WriteAsync(stopLoss, token);
                            }
                            else
                            {
                                _logger.LogWarning("Unable to update trade for {Instrument}", trade.Instrument);
                            }
                        }
                        else if (trade.Price + trade.UnrealizedPL >= trailingStop.StopLoss)
                        {
                            var update = new OrderUpdate(trade.Price);

                            var success = await _apiService.UpdateTrade(update, trade.Id);

                            if (success)
                            {
                                await _liveTradeCache.TrailingStopChannel.Writer.WriteAsync(trailingStop, token);
                            }
                            else
                            {
                                _logger.LogWarning("Unable to update trade for {Instrument}", trade.Instrument);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Trade for {Instrument} is currently at {CurrentPrice}. Waiting to reach {StopLoss}",
                                trade.Instrument, trade.Price + trade.UnrealizedPL, trailingStop.StopLoss);

                            await Task.Delay(tradeSettings.CandleSpan, token);

                            await _liveTradeCache.TrailingStopChannel.Writer.WriteAsync(trailingStop, token);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while trying to update a trade");
                    }
                });

            await Task.Delay(10, stoppingToken);
        }
    }

    private static double GetProfitTarget(double price, double stopLoss, double riskReward, int displayPrecision)
    {
        return Math.Round(stopLoss + (stopLoss - price) * riskReward, displayPrecision);
    }
}