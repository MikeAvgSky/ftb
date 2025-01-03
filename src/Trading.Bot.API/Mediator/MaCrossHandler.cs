namespace Trading.Bot.API.Mediator;

public sealed class MaCrossHandler : IRequestHandler<MovingAverageCrossRequest, IResult>
{
    public Task<IResult> Handle(MovingAverageCrossRequest request, CancellationToken cancellationToken)
    {
        var fileData = new List<FileData<IEnumerable<object>>>();

        var maxSpread = request.MaxSpread ?? 0.0003;

        var minGain = request.MinGain ?? 0.0006;

        var riskReward = request.RiskReward ?? 1;

        var tradeRisk = request.TradeRisk ?? 10;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var maShortList = request.ShortWindow?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse) ?? new[] { 10 };

            var maLongList = request.LongWindow?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse) ?? new[] { 20 };

            var mergedWindows = maShortList.Concat(maLongList).GetAllWindowCombinations().Distinct();

            foreach (var window in mergedWindows)
            {
                var movingAvgCross = candles.CalcMaCross(window.Item1, window.Item2, maxSpread, minGain, riskReward);

                var fileName = $"MaCross_{instrument}_{granularity}_{window.Item1}_{window.Item2}";

                fileData.AddRange(movingAvgCross.Cast<IndicatorResult>().ToArray().GetFileData(fileName, tradeRisk, riskReward));
            }
        }

        if (!fileData.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(Results.File(fileData.GetZipFromFileData(),
            "application/octet-stream", "MaCross.zip"));
    }
}

public record MovingAverageCrossRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public string ShortWindow { get; set; } = "";
    public string LongWindow { get; set; } = "";
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public double? RiskReward { get; set; }
    public int? TradeRisk { get; set; }
}