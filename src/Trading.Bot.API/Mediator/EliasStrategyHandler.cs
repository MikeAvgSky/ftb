namespace Trading.Bot.API.Mediator;

public class EliasStrategyHandler : IRequestHandler<EliasStrategyRequest, IResult>
{
    public Task<IResult> Handle(EliasStrategyRequest request, CancellationToken cancellationToken)
    {
        var fileData = new List<FileData<IEnumerable<object>>>();

        var minGain = request.MinGain ?? 0.001;

        var riskReward = request.RiskReward ?? 1;

        var tradeRisk = request.TradeRisk ?? 10;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var macdEma = candles.CalcEliasStrategy(request.ShortWindow, request.MediumWindow, request.LongWindow,
                request.ResistanceLevel, minGain, request.RiskReward ?? 1, request.MaxSpread ?? 0.0004);

            var fileName = $"EliasStrategy_{instrument}_{granularity}";

            fileData.AddRange(macdEma.GetFileData(fileName, tradeRisk, riskReward, true));
        }

        if (!fileData.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(Results.File(fileData.GetZipFromFileData(), "application/octet-stream",
            "Elias_Strategy.zip"));
    }
}

public record EliasStrategyRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int ShortWindow { get; set; }
    public int MediumWindow { get; set; }
    public int LongWindow { get; set; }
    public int ResistanceLevel { get; set; }
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public double? RiskReward { get; set; }
    public int? TradeRisk { get; set; }
}