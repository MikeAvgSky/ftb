namespace Trading.Bot.Services;

public class TrailingStopManager : BackgroundService
{
    private readonly ILogger<TrailingStopManager> _logger;
    private readonly OandaApiService _apiService;
    private readonly LiveTradeCache _liveTradeCache;
    private readonly TradeSettings[] _tradeSettings;
    private readonly ParallelOptions _options = new();

    public TrailingStopManager(ILogger<TrailingStopManager> logger, OandaApiService apiService,
        LiveTradeCache liveTradeCache, TradeConfiguration tradeConfiguration)
    {
        _logger = logger;
        _apiService = apiService;
        _liveTradeCache = liveTradeCache;
        _tradeSettings = tradeConfiguration.TradeSettings;
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
                        var trade = await _apiService.GetTrade(trailingStop.TradeId);

                        if (trade is null || trade.State != "OPEN")
                        {
                            _logger.LogInformation("Trade {TradeId} not found or not open", trailingStop.TradeId);
                            return;
                        }

                        var settings = _tradeSettings.First(s => s.Instrument == trade.Instrument);

                        await Task.Delay(settings.CandleSpan, token);

                        await DetectTrailingStop(trailingStop, trade, settings.RiskReward);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while trying to update a trade");
                    }
                });

            await Task.Delay(10, stoppingToken);
        }
    }

    private async Task DetectTrailingStop(TrailingStop trailingStop, TradeResponse trade, double riskReward)
    {
        if (StopLossTargetExceeded(trailingStop, trade.Instrument))
        {
            var update = new OrderUpdate(trailingStop.DisplayPrecision, trailingStop.StopLossTarget);

            var newTrailingStop = new TrailingStop
            {
                TradeId = trade.Id,
                StopLossTarget = GetNewTarget(update.StopLoss.Price, trade.Price, riskReward),
                DisplayPrecision = trailingStop.DisplayPrecision
            };

            await TryUpdateTrade(newTrailingStop, trade, update);
        }
        else
        {
            await _liveTradeCache.TrailingStopChannel.Writer.WriteAsync(trailingStop);
        }
    }

    private bool StopLossTargetExceeded(TrailingStop trailingStop, string instrument)
    {
        var currentPrice = trailingStop.Signal switch
        {
            Signal.Buy => _liveTradeCache.LivePrices[instrument].Bid,
            Signal.Sell => _liveTradeCache.LivePrices[instrument].Ask,
            _ => 0.0
        };

        return currentPrice > Math.Round(trailingStop.StopLossTarget, trailingStop.DisplayPrecision);
    }

    private async Task TryUpdateTrade(TrailingStop newTrailingStop, TradeResponse trade, OrderUpdate update)
    {
        var success = await _apiService.UpdateTrade(update, trade.Id);

        if (success)
        {
            await _liveTradeCache.TrailingStopChannel.Writer.WriteAsync(newTrailingStop);
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