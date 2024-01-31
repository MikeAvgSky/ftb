namespace Trading.Bot.Mediator;

public sealed class CalculateMovingAverageHandler : IRequestHandler<CalculateMovingAverageRequest, IResult>
{
    private readonly OandaApiService _apiService;

    public CalculateMovingAverageHandler(OandaApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IResult> Handle(CalculateMovingAverageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.MaShort) || string.IsNullOrEmpty(request.MaLong))
        {
            return Results.BadRequest("Please provide short and/or long windows");
        }

        var candles = request.File.GetObjectFromCsv<Candle>();

        if (!candles.Any()) return Results.Empty;

        var instruments = request.File.FileName[..request.File.FileName.LastIndexOf('_')];

        var instrumentInfo = (await _apiService.GetInstrumentsFromOanda(instruments)).First();

        var maShortList = request.MaShort.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);

        var maLongList = request.MaLong.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);

        var mergedWindows = maShortList.Concat(maLongList).GetAllWindowCombinations().Distinct();

        var movingAvgCrossList = new List<FileData<IEnumerable<MovingAverageCross>>>();

        foreach (var window in mergedWindows)
        {
            var maShort = candles.Select(c => c.Mid_C).SimpleMovingAverage(window.Item1).ToList();

            var maLong = candles.Select(c => c.Mid_C).SimpleMovingAverage(window.Item2).ToList();

            var movingAvgCross = CreateMovingAverageCross(candles, maShort, maLong, instrumentInfo);

            movingAvgCrossList.Add(new FileData<IEnumerable<MovingAverageCross>>(
                $"{instruments}_MA_{window.Item1}_{window.Item2}.csv", 
                request.ShowTradesOnly ? movingAvgCross.Where(ma => ma.Trade != Trade.None) : movingAvgCross));
        }

        return request.Download
            ? Results.File(movingAvgCrossList.GetZipFromFileData(),
                "application/octet-stream", $"{instruments}_MA.zip")
            : Results.Ok(movingAvgCrossList.Select(l => l.Value));
    }

    private static IEnumerable<MovingAverageCross> CreateMovingAverageCross(IEnumerable<Candle> candles, 
        IReadOnlyList<double> maShort, IReadOnlyList<double> maLong, Instrument instrumentInfo)
    {
        var movingAvgCross = candles.Select(c => new MovingAverageCross(c)).ToList();

        var cumGain = 0.0;

        for (var i = 0; i < movingAvgCross.Count; i++)
        {
            movingAvgCross[i].MaShort = maShort[i];
            movingAvgCross[i].MaLong = maLong[i];
            movingAvgCross[i].Delta = maShort[i] - maLong[i];
            movingAvgCross[i].DeltaPrev = i > 0 ? movingAvgCross[i - 1].Delta : 0;
            movingAvgCross[i].Trade = movingAvgCross[i].Delta switch
            {
                >= 0 when movingAvgCross[i].DeltaPrev < 0 => Trade.Buy,
                < 0 when movingAvgCross[i].DeltaPrev >= 0 => Trade.Sell,
                _ => Trade.None
            };
            movingAvgCross[i].Diff = i < movingAvgCross.Count - 1
                ? movingAvgCross[i + 1].Candle.Mid_C - movingAvgCross[i].Candle.Mid_C
                : movingAvgCross[i].Candle.Mid_C;
            movingAvgCross[i].Gain = movingAvgCross[i].Diff / instrumentInfo.PipLocation *
                                     GetTradeValue(movingAvgCross[i].Trade);
            cumGain += movingAvgCross[i].Gain;
            movingAvgCross[i].CumGain = cumGain;
        }

        return movingAvgCross;
    }

    private static int GetTradeValue(Trade trade)
    {
        return trade switch
        {
            Trade.None => 0,
            Trade.Buy => 1,
            Trade.Sell => -1,
            _ => 0,
        };
    }
}

public record CalculateMovingAverageRequest : IHttpRequest
{
    public IFormFile File { get; set; }
    public string MaShort { get; set; }
    public string MaLong { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}