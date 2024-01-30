namespace Trading.Bot.Mediator;

public class CalculateMovingAverageCrossHandler : IRequestHandler<CalculateMovingAverageCrossRequest, IResult>
{
    private readonly OandaApiService _apiService;

    public CalculateMovingAverageCrossHandler(OandaApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IResult> Handle(CalculateMovingAverageCrossRequest request, CancellationToken cancellationToken)
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

        var mergedWindows = maShortList.Concat(maLongList).GetAllWindowCombinations().Distinct().ToList();

        var movingAvgCrossList = new List<FileData<IEnumerable<MovingAverageCross>>>();

        foreach (var window in mergedWindows)
        {
            var movingAvgCross = candles.Select(c => new MovingAverageCross(c)).ToList();

            var maShort = candles.Select(c => c.Mid_C).SimpleMovingAverage(window.Item1).ToList();

            var maLong = candles.Select(c => c.Mid_C).SimpleMovingAverage(window.Item2).ToList();

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
            }

            movingAvgCrossList.Add(new FileData<IEnumerable<MovingAverageCross>>(
                $"{instruments}_Moving_Average_Cross_{window.Item1}_{window.Item2}.csv",
                movingAvgCross.Where(m => m.Trade != Trade.None)));
        }

        return request.Download
            ? Results.File(movingAvgCrossList.GetZipFromFileData(),
                "application/octet-stream", "moving_average_cross.zip")
            : Results.Ok(candles);
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

public record CalculateMovingAverageCrossRequest : IHttpRequest
{
    public IFormFile File { get; set; }
    public string MaShort { get; set; }
    public string MaLong { get; set; }
    public bool Download { get; set; }
}