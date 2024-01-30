namespace Trading.Bot.Mediator;

public sealed class GetCandlesHandler : IRequestHandler<GetCandlesRequest, IResult>
{
    private readonly OandaApiService _apiService;

    public GetCandlesHandler(OandaApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IResult> Handle(GetCandlesRequest request, CancellationToken cancellationToken)
    {
        if (!request.Currencies.Contains(','))
        {
            return Results.BadRequest("Please provide comma separated currencies");
        }

        var currencyList = request.Currencies.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var instruments = currencyList.GetAllCombinations();

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
            var candles = (await _apiService.GetCandlesFromOanda(
                    instrument, request.Granularity, request.Price, count, fromDate, toDate))
                .ToList();

            if (candles.Any())
            {
                candlesBag.Add(new FileData<IEnumerable<Candle>>(
                    $"{instrument}_{request.Granularity ?? OandaApiService.defaultGranularity}.csv", candles));
            }
        });

        return request.Download
            ? Results.File(candlesBag.GetZipFromFileData(),
                "application/octet-stream", "candles.zip")
            : Results.Ok(candlesBag.Select(bag => bag.Value));
    }
}

public record GetCandlesRequest : IHttpRequest
{
    public string Currencies { get; set; }
    public string Granularity { get; set; }
    public string Price { get; set; }
    public string FromDate { get; set; }
    public string ToDate { get; set; }
    public string Count { get; set; }
    public bool Download { get; set; }
}