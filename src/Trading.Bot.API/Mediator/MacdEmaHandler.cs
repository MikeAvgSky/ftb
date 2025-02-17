namespace Trading.Bot.API.Mediator;

public class MacdEmaHandler : IRequestHandler<MacdEmaRequest, IResult>
{
    public Task<IResult> Handle(MacdEmaRequest request, CancellationToken cancellationToken)
    {
        var fileData = new List<FileData<IEnumerable<object>>>();

        var maxSpread = request.MaxSpread ?? 0.0003m;

        var minGain = request.MinGain ?? 0.0006m;

        var riskReward = request.RiskReward ?? 1;

        var tradeRisk = request.TradeRisk ?? 10;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var macdEma = candles.CalcMacdEma(request.EmaWindow, maxSpread, minGain, riskReward);

            var fileName = $"MacdEma_{instrument}_{granularity}_{request.EmaWindow}";

            fileData.AddRange(macdEma.GetFileData(fileName, tradeRisk, riskReward));
        }

        if (!fileData.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(Results.File(fileData.GetZipFromFileData(),
            "application/octet-stream", "MacdEma.zip"));
    }
}

public record MacdEmaRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int EmaWindow { get; set; }
    public decimal? MaxSpread { get; set; }
    public decimal? MinGain { get; set; }
    public decimal? RiskReward { get; set; }
    public int? TradeRisk { get; set; }
}