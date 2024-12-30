namespace Trading.Bot.API.Mediator;

public class MacdEmaHandler : IRequestHandler<MacdEmaRequest, IResult>
{
    public Task<IResult> Handle(MacdEmaRequest request, CancellationToken cancellationToken)
    {
        var macdEmaList = new List<FileData<IEnumerable<object>>>();

        var maxSpread = request.MaxSpread ?? 0.0003;

        var minGain = request.MinGain ?? 0.002;

        var riskReward = request.RiskReward ?? 1;

        var tradeRisk = request.TradeRisk ?? 10;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var macdEma = candles.CalcMacdEma(request.EmaWindow, maxSpread, minGain, riskReward);

            var tradingSim = TradeResult.SimulateTrade(macdEma.Cast<IndicatorBase>().ToArray(), tradeRisk, riskReward);

            macdEmaList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_MacdEma_{request.EmaWindow}.csv",
                macdEma.Where(ma => ma.Signal != Signal.None)));

            macdEmaList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_MacdEma_{request.EmaWindow}_Simulation.csv", tradingSim.Result));

            macdEmaList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_MacdEma_{request.EmaWindow}_Summary.csv", new[] { tradingSim.Summary }));
        }

        if (!macdEmaList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(Results.File(macdEmaList.GetZipFromFileData(),
            "application/octet-stream", "MacdEma.zip"));
    }
}

public record MacdEmaRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int EmaWindow { get; set; }
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public double? RiskReward { get; set; }
    public int? TradeRisk { get; set; }
}