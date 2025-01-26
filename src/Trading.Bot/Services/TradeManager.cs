namespace Trading.Bot.Services;

public class TradeManager : BackgroundService
{
    private readonly ILogger<TradeManager> _logger;
    private readonly OandaApiService _apiService;
    private readonly LiveTradeCache _liveTradeCache;
    private readonly TradeConfiguration _tradeConfiguration;
    private readonly EmailService _emailService;
    private readonly List<Instrument> _instruments = new();
    private readonly ParallelOptions _options = new();

    public TradeManager(ILogger<TradeManager> logger, OandaApiService apiService,
        LiveTradeCache liveTradeCache, TradeConfiguration tradeConfiguration, EmailService emailService)
    {
        _logger = logger;
        _apiService = apiService;
        _liveTradeCache = liveTradeCache;
        _tradeConfiguration = tradeConfiguration;
        _emailService = emailService;
        _options.MaxDegreeOfParallelism = _tradeConfiguration.TradeSettings.Length;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Initialise();

        await StartTrading(stoppingToken);
    }

    private async Task Initialise()
    {
        _instruments.AddRange(await _apiService.GetInstruments(string.Join(",",
            _tradeConfiguration.TradeSettings.Select(s => s.Instrument))));
    }

    private async Task StartTrading(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Parallel.ForEachAsync(_liveTradeCache.LivePriceChannel.Reader.ReadAllAsync(stoppingToken),
                _options, async (price, token) =>
                {
                    try
                    {
                        await DetectNewTrade(price, token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while trying to calculate and execute a trade");
                    }
                });

            await Task.Delay(10, stoppingToken);
        }
    }

    private async Task DetectNewTrade(LivePrice price, CancellationToken stoppingToken)
    {
        var settings = _tradeConfiguration.TradeSettings.First(x => x.Instrument == price.Instrument);

        if (!await NewCandleAvailable(settings, price, stoppingToken) || !GoodTradingTime()) return;

        var candles = await _apiService.GetCandles(settings.Instrument, settings.MainGranularity);

        if (candles.Length == 0)
        {
            _logger.LogInformation("Not placing a trade for {Instrument}, candles not found", settings.Instrument);
            return;
        }

        var calcResult = candles.CalcMikeStrategy(settings.Integers[0], settings.MaxSpread, settings.MinGain, settings.RiskReward).Last();

        await UpdateWinningTrades(settings, calcResult);

        if (calcResult.Signal != Signal.None)
        {
            await TryPlaceTrade(settings, calcResult);
            return;
        }

        _logger.LogInformation("Not placing a trade for {Instrument} based on the indicator", settings.Instrument);
    }

    private static bool GoodTradingTime()
    {
        var date = DateTime.UtcNow;

        return date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
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

        var currentTime = await _apiService.GetLastCandleTime(settings.Instrument, settings.MainGranularity);

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

    private async Task TryPlaceTrade(TradeSettings settings, IndicatorBase indicator)
    {
        if (!await CanPlaceTrade(settings))
        {
            _logger.LogInformation("Cannot place trade for {Instrument}, already open.", settings.Instrument);
            return;
        }

        var instrument = _instruments.FirstOrDefault(i => i.Name == settings.Instrument);

        if (instrument is null) return;

        var tradeUnits = await GetTradeUnits(settings, indicator);

        var trailingStop = settings.TrailingStop ? CalcTrailingStop(indicator) : 0;

        var order = new Order(instrument, tradeUnits, indicator.Signal, indicator.StopLoss, indicator.TakeProfit, trailingStop);

        var ofResponse = await _apiService.PlaceTrade(order);

        if (ofResponse is null)
        {
            _logger.LogWarning("Failed to place order for {Instrument}", settings.Instrument);
            return;
        }

        if (_tradeConfiguration.SendEmail)
        {
            await SendEmailNotification(new
            {
                ofResponse.Instrument,
                Signal = indicator.Signal.ToString(),
                ofResponse.TradeOpened.Units,
                ofResponse.TradeOpened.Price,
                TakeProfit = order.TakeProfitOnFill?.Price ?? 0,
                StopLoss = order.StopLossOnFill?.Price ?? 0,
                TrailingStop = order.TrailingStopLossOnFill?.Distance ?? 0
            });
        }
    }

    private static double CalcTrailingStop(IndicatorBase indicator)
    {
        return indicator.Gain * 1.5;
    }

    private async Task SendEmailNotification(object emailBody)
    {
        await _emailService.SendMailAsync(new EmailData
        {
            EmailToAddress = "mike.avgeros@gmail.com",
            EmailToName = "Mike",
            EmailSubject = "New Trade",
            EmailBody = JsonSerializer.Serialize(emailBody, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            })
        });
    }

    private async Task<double> GetTradeUnits(TradeSettings settings, IndicatorBase indicator)
    {
        var price = (await _apiService.GetPrices(settings.Instrument)).FirstOrDefault();

        if (price is null) return 0.0;

        var pipLocation = _instruments.FirstOrDefault(i => i.Name == settings.Instrument)?.PipLocation ?? 1.0;

        var numPips = indicator.Loss / pipLocation;

        var perPipLoss = _tradeConfiguration.TradeRisk / numPips;

        return perPipLoss / (price.HomeConversion * pipLocation);
    }

    private async Task<bool> CanPlaceTrade(TradeSettings settings)
    {
        var openTrades = await _apiService.GetOpenTrades();

        return openTrades.All(ot => ot.Instrument != settings.Instrument);
    }

    private async Task UpdateWinningTrades(TradeSettings settings, IndicatorBase indicator)
    {
        var openTrade = (await _apiService.GetOpenTrades()).FirstOrDefault(ot => ot.Instrument == settings.Instrument);

        if (openTrade is null) return;

        var openTradeSignal = openTrade.InitialUnits > 0 ? Signal.Buy : Signal.Sell;

        if (indicator.Signal != Signal.None && openTrade.UnrealizedPL > 0 && DifferentDirection(indicator.Signal, openTradeSignal))
        {
            await _apiService.CloseTrade(openTrade.Id);
            return;
        }

        if (ShouldUpdateStopLoss(openTrade, indicator))
        {
            await _apiService.UpdateTrade(new OrderUpdate(stopLoss: openTrade.Price), openTrade.Id);
        }
    }

    private static bool ShouldUpdateStopLoss(TradeResponse trade, IndicatorBase indicator)
    {
        var priceList = new List<double> { trade.Price, trade.TakeProfitOrder.Price };

        var currentValue = trade.InitialUnits > 0
            ? indicator.Candle.Ask_C
            : indicator.Candle.Bid_C;

        var closest = priceList.OrderBy(value => Math.Abs(currentValue - value)).First();

        return trade.StopLossOrder.Price < trade.Price && trade.TakeProfitOrder.Price - closest == 0;
    }

    private static bool DifferentDirection(Signal signal, Signal tradeSignal) => signal != tradeSignal;
}