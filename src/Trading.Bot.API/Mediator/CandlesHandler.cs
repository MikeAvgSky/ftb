namespace Trading.Bot.API.Mediator;

public sealed class CandlesHandler : IRequestHandler<CandlesRequest, IResult>
{
    private readonly OandaApiService _apiService;

    public CandlesHandler(OandaApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IResult> Handle(CandlesRequest request, CancellationToken cancellationToken)
    {
        if (!request.Currencies.Contains(','))
        {
            return Results.BadRequest("Please provide comma separated currencies");
        }

        var currencyList = request.Currencies.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var instruments = currencyList.GetAllCombinations();

        var granularities = request.Granularity.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (!granularities.Any())
        {
            granularities = new[] { OandaApiService.DefaultGranularity };
        }

        var candlesBag = new ConcurrentBag<FileData<IEnumerable<Candle>>>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3
        };

        DateTime.TryParse(request.FromDate, out var fromDate);

        DateTime.TryParse(request.ToDate, out var toDate);

        var count = int.TryParse(request.Count, out var _count) ? _count : 500;

        await Parallel.ForEachAsync(instruments, parallelOptions, async (instrument, _) =>
        {
            foreach (var granularity in granularities)
            {
                var candles = (await _apiService.GetCandles(
                        instrument, granularity, request.Price, count, fromDate, toDate)).ToList();

                if (candles.Any())
                {
                    if (toDate != default && candles.Last().Time < toDate)
                    {
                        while (candles.Last().Time < toDate)
                        {
                            candles.AddRange(await _apiService.GetCandles(
                                instrument, request.Granularity, request.Price, count, candles.Last().Time, toDate));
                        }

                        if (candles.Last().Time > toDate) candles.RemoveAll(c => c.Time > toDate);

                        candlesBag.Add(new FileData<IEnumerable<Candle>>(
                            $"{instrument}_{granularity}.csv",
                            candles.DistinctBy(c => c.Time)));
                    }
                    else
                    {
                        candlesBag.Add(new FileData<IEnumerable<Candle>>(
                            $"{instrument}_{granularity}.csv", candles));
                    }
                }
            }
        });

        return request.Download
            ? Results.File(candlesBag.GetZipFromFileData(),
                "application/octet-stream", "candles.zip")
            : Results.Ok(candlesBag.Select(bag => bag.Value));
    }
}

public record CandlesRequest : IHttpRequest
{
    public string Currencies { get; set; } = "";
    public string Granularity { get; set; } = "";
    public string Price { get; set; } = "";
    public string FromDate { get; set; } = "";
    public string ToDate { get; set; } = "";
    public string Count { get; set; } = "";
    public bool Download { get; set; }
}