namespace Trading.Bot.Mediator;

public sealed class MovingAverageCrossHandler : IRequestHandler<MovingAverageCrossRequest, IResult>
{
    public Task<IResult> Handle(MovingAverageCrossRequest crossRequest, CancellationToken cancellationToken)
    {
        var movingAvgCrossList = new List<FileData<IEnumerable<MovingAverageCross>>>();

        foreach (var file in crossRequest.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var maShortList = crossRequest.ShortWindow?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
                              ?? new[] { 10 };

            var maLongList = crossRequest.LongWindow?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
                             ?? new[] { 20 };

            var mergedWindows = maShortList.Concat(maLongList).GetAllWindowCombinations().Distinct();

            foreach (var window in mergedWindows)
            {
                var tradeSettings = new TradeSettings { ShortWindow = window.Item1, LongWindow = window.Item2 };

                var movingAvgCross = MovingAverageCross.ProcessCandles(candles, tradeSettings);

                movingAvgCrossList.Add(new FileData<IEnumerable<MovingAverageCross>>(
                    $"{instrument}_{granularity}_MA_{window.Item1}_{window.Item2}.csv",
                    crossRequest.ShowTradesOnly ? movingAvgCross.Where(ma => ma.Signal != Signal.None) : movingAvgCross));
            }
        }

        if (!movingAvgCrossList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(crossRequest.Download
            ? Results.File(movingAvgCrossList.GetZipFromFileData(),
                "application/octet-stream", "ma.zip")
            : Results.Ok(movingAvgCrossList.Select(l => l.Value)));
    }
}

public record MovingAverageCrossRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; }
    public string ShortWindow { get; set; }
    public string LongWindow { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}