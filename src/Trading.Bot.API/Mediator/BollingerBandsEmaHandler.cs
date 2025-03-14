namespace Trading.Bot.API.Mediator;

public class BollingerBandsEmaHandler : IRequestHandler<BollingerBandsEmaRequest, IResult>
{
    public Task<IResult> Handle(BollingerBandsEmaRequest request, CancellationToken cancellationToken)
    {
        var fileData = new List<FileData<IEnumerable<object>>>();

        var rsiLow = request.RsiLow ?? 30;

        var rsiHigh = request.RsiHigh ?? 70;

        var maxSpread = request.MaxSpread ?? 0.0003m;

        var minGain = request.MinGain ?? 0;

        var riskReward = request.RiskReward ?? 1;

        var tradeRisk = request.TradeRisk ?? 10;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (candles.Length == 0) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var bollingerBands = candles.CalcTrendMomentum(request.Window, request.EmaWindow,
                request.StandardDeviation, rsiLow, rsiHigh, maxSpread, minGain, riskReward);

            var fileName = $"TrendMomentum_{instrument}_{granularity}_{request.Window}_{request.EmaWindow}_{request.StandardDeviation}";

            fileData.AddRange(bollingerBands.GetFileData(fileName, tradeRisk, riskReward));
        }

        if (fileData.Count == 0) return Task.FromResult(Results.Empty);

        return Task.FromResult(Results.File(fileData.GetZipFromFileData(),
            "application/octet-stream", "TrendMomentum.zip"));
    }
}

public record BollingerBandsEmaRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int Window { get; set; }
    public int EmaWindow { get; set; }
    public double StandardDeviation { get; set; }
    public double? RsiLow { get; set; }
    public double? RsiHigh { get; set; }
    public decimal? MaxSpread { get; set; }
    public decimal? MinGain { get; set; }
    public decimal? RiskReward { get; set; }
    public int? TradeRisk { get; set; }
}