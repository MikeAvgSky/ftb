namespace Trading.Bot.Mediator;

public sealed class CalculateMovingAverageHandler : IRequestHandler<CalculateMovingAverageRequest, IResult>
{
    public Task<IResult> Handle(CalculateMovingAverageRequest request, CancellationToken cancellationToken)
    {
        var movingAvgCrossList = new List<FileData<IEnumerable<MovingAverageCross>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var maShortList = request.MaShort?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
                              ?? new[] { 10 };

            var maLongList = request.MaLong?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
                             ?? new[] { 20 };

            var mergedWindows = maShortList.Concat(maLongList).GetAllWindowCombinations().Distinct();

            foreach (var window in mergedWindows)
            {
                var tradeSettings = new TradeSettings { ShortWindow = window.Item1, LongWindow = window.Item2 };

                var movingAvgCross = MovingAverageCross.ProcessCandles(candles, tradeSettings);

                movingAvgCrossList.Add(new FileData<IEnumerable<MovingAverageCross>>(
                    $"{instrument}_{granularity}_MA_{window.Item1}_{window.Item2}.csv",
                    request.ShowTradesOnly ? movingAvgCross.Where(ma => ma.Signal != Signal.None) : movingAvgCross));
            }
        }

        if (!movingAvgCrossList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(request.Download
            ? Results.File(movingAvgCrossList.GetZipFromFileData(),
                "application/octet-stream", "ma.zip")
            : Results.Ok(movingAvgCrossList.Select(l => l.Value)));
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