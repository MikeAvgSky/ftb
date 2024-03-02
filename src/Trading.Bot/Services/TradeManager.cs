namespace Trading.Bot.Services;

public class TradeManager : BackgroundService
{
    private readonly OandaApiService _apiService;
    private readonly LivePriceCache _livePriceCache;
    private readonly ILogger<TradeManager> _logger;
    private readonly TradeConfiguration _tradeConfiguration;
    private readonly SemaphoreSlim _semaphore;
    private readonly List<Instrument> _instruments = new();

    public TradeManager(OandaApiService apiService, LivePriceCache livePriceCache, 
        ILogger<TradeManager> logger, TradeConfiguration tradeConfiguration)
    {
        _apiService = apiService;
        _livePriceCache = livePriceCache;
        _logger = logger;
        _tradeConfiguration = tradeConfiguration;
        _semaphore = new SemaphoreSlim(_tradeConfiguration.TradeSettings.Length);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Initialise();

        await StartTrading(stoppingToken);
    }

    private async Task StartTrading(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();

        while (!stoppingToken.IsCancellationRequested)
        {
            while (_livePriceCache.LivePriceQueue.Count != 0)
            {
                await _semaphore.WaitAsync(stoppingToken);

                if (!_livePriceCache.LivePriceQueue.TryDequeue(out var price))
                {
                    _semaphore.Release();
                    continue;
                }

                _logger.LogInformation($"New price found {price.Instrument}");

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var settings = _tradeConfiguration.TradeSettings.First(x => x.Instrument == price.Instrument);

                        if (!await NewCandleAvailable(settings, price, stoppingToken))
                        {
                            _semaphore.Release();
                            return;
                        }

                        var candles =
                            await _apiService.GetCandles(settings.Instrument, settings.Granularity, count: settings.MovingAverage * 2);

                        if (!candles.Any()) return;

                        var calcResult = candles.CalcBollingerBands(settings.MovingAverage, settings.StandardDeviation,
                            settings.MaxSpread, settings.MinGain, settings.RiskReward).Last();

                        if (calcResult.Signal != Signal.None)
                        {
                            await TryPlaceTrade(settings, calcResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while trying to calculate and execute a trade");
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, stoppingToken));

                tasks.RemoveAll(p => p.IsCompleted);

                Thread.Sleep(10);
            }
        }
    }

    private async Task<bool> NewCandleAvailable(TradeSettings settings, LivePrice price, CancellationToken stoppingToken)
    {
        var retryCount = 0;

        Start:

        if (retryCount >= 10)
        {
            _logger.LogWarning("Cannot get candle that matches the live price. Giving up.");
            return false;
        }

        var currentTime = await _apiService.GetLastCandleTime(settings.Instrument, settings.Granularity);

        if (TimeMatches(price.Time, currentTime)) return true;

        await Task.Delay(1000, stoppingToken);

        retryCount++;

        goto Start;
    }

    private static bool TimeMatches(DateTime priceTime, DateTime currentTime)
    {
        return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute,
                   currentTime.Second) ==
               new DateTime(priceTime.Year, priceTime.Month, priceTime.Day, priceTime.Hour, priceTime.Minute,
                   priceTime.Second);
    }

    private async Task Initialise()
    {
        _instruments.AddRange(await _apiService.GetInstruments(string.Join(",",
            _tradeConfiguration.TradeSettings.Select(s => s.Instrument))));
    }

    private async Task TryPlaceTrade(TradeSettings settings, IndicatorBase indicator)
    {
        if (!await CanPlaceTrade(settings))
        {
            _logger.LogInformation($"Cannot place trade for {settings.Instrument}, already open.");
            return;
        }

        var instrument = _instruments.FirstOrDefault(i => i.Name == settings.Instrument);

        if (instrument is null) return;

        var tradeUnits = await GetTradeUnits(settings, indicator);

        var ofResponse = await _apiService.PlaceTrade(
            new Order(instrument, tradeUnits, indicator.Signal, indicator.StopLoss, indicator.TakeProfit));

        if (ofResponse is null)
        {
            _logger.LogWarning($"Failed to place order for {settings.Instrument}");
            return;
        }

        _logger.LogInformation($"Successfully placed order for {ofResponse.Instrument} with id {ofResponse.Id} ");
    }

    private async Task<double> GetTradeUnits(TradeSettings settings, IndicatorBase indicator)
    {
        var price = (await _apiService.GetPrices(settings.Instrument)).FirstOrDefault();

        if (price is null) return 0.0;

        var conversionFactor = indicator.Signal switch
        {
            Signal.Buy => price.HomeConversion,
            Signal.Sell => price.HomeConversion,
            _ => 1.0
        };

        var pipLocation = _instruments.FirstOrDefault(i => i.Name == settings.Instrument)?.PipLocation ?? 1.0;

        var numPips = indicator.Loss / pipLocation;

        var perPipLoss = _tradeConfiguration.TradeRisk / numPips;

        return perPipLoss / (conversionFactor * pipLocation);
    }

    private async Task<bool> CanPlaceTrade(TradeSettings settings)
    {
        var openTrades = await _apiService.GetOpenTrades();

        return openTrades.All(ot => ot.Instrument != settings.Instrument);
    }
}