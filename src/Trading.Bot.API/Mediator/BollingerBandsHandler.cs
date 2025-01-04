namespace Trading.Bot.API.Mediator;

public class BollingerBandsHandler : IRequestHandler<BollingerBandsRequest, IResult>
{
    public Task<IResult> Handle(BollingerBandsRequest request, CancellationToken cancellationToken)
    {
        var fileData = new List<FileData<IEnumerable<object>>>();

        var rsiLow = request.RsiLow ?? 30;

        var rsiHigh = request.RsiHigh ?? 70;

        var maxSpread = request.MaxSpread ?? 0.0003;

        var minGain = request.MinGain ?? 0;

        var riskReward = request.RiskReward ?? 1;

        var tradeRisk = request.TradeRisk ?? 10;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var bollingerBands = candles.CalcMeanReversion(request.Window, request.StandardDeviation,
                rsiLow, rsiHigh, maxSpread, minGain, riskReward);

            var fileName = $"MeanReversion_{instrument}_{granularity}_{request.Window}_{request.StandardDeviation}";

            fileData.AddRange(bollingerBands.GetFileData(fileName, tradeRisk, riskReward));
        }

        if (!fileData.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(Results.File(fileData.GetZipFromFileData(),
            "application/octet-stream", "MeanReversion.zip"));
    }
}

public record BollingerBandsRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int Window { get; set; }
    public double StandardDeviation { get; set; }
    public double? RsiLow { get; set; }
    public double? RsiHigh { get; set; }
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public double? RiskReward { get; set; }
    public int? TradeRisk { get; set; }
}