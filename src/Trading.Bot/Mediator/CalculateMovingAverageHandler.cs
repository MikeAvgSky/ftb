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
        var movingAvgCrossList = new List<FileData<IEnumerable<MovingAverageCross>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var instrumentInfo = (await _apiService.GetInstrumentsFromOanda(instrument)).First();

            var maShortList = request.MaShort?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
                              ?? new[] { 10 };

            var maLongList = request.MaLong?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
                             ?? new[] { 20 };

            var mergedWindows = maShortList.Concat(maLongList).GetAllWindowCombinations().Distinct();

            foreach (var window in mergedWindows)
            {
                var maShort = candles.Select(c => c.Mid_C).MovingAverage(window.Item1).ToList();

                var maLong = candles.Select(c => c.Mid_C).MovingAverage(window.Item2).ToList();

                var movingAvgCross = CreateMovingAverageCross(candles, maShort, maLong, instrumentInfo);

                movingAvgCrossList.Add(new FileData<IEnumerable<MovingAverageCross>>(
                    $"{instrument}_{granularity}_MA_{window.Item1}_{window.Item2}.csv",
                    request.ShowTradesOnly ? movingAvgCross.Where(ma => ma.Trade != Signal.None) : movingAvgCross));
            }
        }

        if (!movingAvgCrossList.Any()) return Results.Empty;

        return request.Download
            ? Results.File(movingAvgCrossList.GetZipFromFileData(),
                "application/octet-stream", "ma.zip")
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
                >= 0 when movingAvgCross[i].DeltaPrev < 0 => Signal.Buy,
                < 0 when movingAvgCross[i].DeltaPrev >= 0 => Signal.Sell,
                _ => Signal.None
            };

            movingAvgCross[i].Diff = i < movingAvgCross.Count - 1
                ? movingAvgCross[i + 1].Candle.Mid_C - movingAvgCross[i].Candle.Mid_C
                : movingAvgCross[i].Candle.Mid_C;

            movingAvgCross[i].Gain = movingAvgCross[i].Diff / instrumentInfo.PipLocation *
                                     movingAvgCross[i].Trade.GetTradeValue();

            cumGain += movingAvgCross[i].Gain;

            movingAvgCross[i].CumGain = cumGain;
        }

        return movingAvgCross;
    }
}

public record CalculateMovingAverageRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; }
    public string MaShort { get; set; }
    public string MaLong { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}