namespace Trading.Bot.API.Mediator;

public class MikeStrategyHandler : IRequestHandler<MikeStrategyRequest, IResult>
{
    public Task<IResult> Handle(MikeStrategyRequest request, CancellationToken cancellationToken)
    {
        var fileData = new List<FileData<IEnumerable<object>>>();

        var maxSpread = request.MaxSpread ?? 0.0004;

        var minGain = request.MinGain ?? 0.001;

        var riskReward = request.RiskReward ?? 1;

        var tradeRisk = request.TradeRisk ?? 20;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var nextCandle = candles.CalcMikeStrategy(request.MovingAverage, maxSpread, minGain, riskReward);

            var fileName = $"MikeStrategy_{instrument}_{granularity}";

            fileData.AddRange(nextCandle.GetFileData(fileName, tradeRisk, riskReward, true));
        }

        if (!fileData.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(Results.File(fileData.GetZipFromFileData(),
            "application/octet-stream", "Mike_Strategy.zip"));
    }
}

public record MikeStrategyRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int MovingAverage { get; set; }
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public double? RiskReward { get; set; }
    public int? TradeRisk { get; set; }
}