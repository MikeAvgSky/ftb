namespace Trading.Bot.API.Mediator;

public sealed class MacHandler : IRequestHandler<MovingAverageCrossRequest, IResult>
{
    public Task<IResult> Handle(MovingAverageCrossRequest request, CancellationToken cancellationToken)
    {
        var movingAvgCrossList = new List<FileData<IEnumerable<object>>>();

        var maxSpread = request.MaxSpread ?? 0.0004;

        var minGain = request.MinGain ?? 0.0006;

        var riskReward = request.RiskReward ?? 1;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var maShortList = request.ShortWindow?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
                              ?? new[] { 10 };

            var maLongList = request.LongWindow?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)
                             ?? new[] { 20 };

            var mergedWindows = maShortList.Concat(maLongList).GetAllWindowCombinations().Distinct();

            foreach (var window in mergedWindows)
            {
                var movingAvgCross = candles.CalcMaCross(window.Item1, window.Item2, maxSpread, minGain, riskReward);

                var tradingSim = TradeResult.SimulateTrade(movingAvgCross.Cast<IndicatorBase>().ToArray());

                movingAvgCrossList.Add(new FileData<IEnumerable<object>>(
                    $"{instrument}_{granularity}_MA_{window.Item1}_{window.Item2}.csv",
                    request.ShowTradesOnly ? movingAvgCross.Where(ma => ma.Signal != Signal.None) : movingAvgCross));

                movingAvgCrossList.Add(new FileData<IEnumerable<object>>(
                    $"{instrument}_{granularity}_MA_{window.Item1}_{window.Item2}_SIM.csv", tradingSim));
            }
        }

        if (!movingAvgCrossList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(request.Download
            ? Results.File(movingAvgCrossList.GetZipFromFileData(),
                "application/octet-stream", "mac.zip")
            : Results.Ok(movingAvgCrossList.Select(l => l.Value)));
    }
}

public record MovingAverageCrossRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public string ShortWindow { get; set; } = "";
    public string LongWindow { get; set; } = "";
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public int? RiskReward { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}