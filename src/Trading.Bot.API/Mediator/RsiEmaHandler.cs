namespace Trading.Bot.API.Mediator;

public class RsiEmaRequestHandler : IRequestHandler<RsiEmaRequest, IResult>
{
    public Task<IResult> Handle(RsiEmaRequest request, CancellationToken cancellationToken)
    {
        var rsiList = new List<FileData<IEnumerable<object>>>();

        var rsiLimit = request.RsiLimit ?? 50;

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

            var rsi = candles.CalcRsiEma(request.RsiWindow, request.EmaWindow, rsiLimit, maxSpread, minGain, riskReward);

            var tradingSim = TradeResult.SimulateTrade(rsi.Cast<IndicatorBase>().ToArray(), tradeRisk, riskReward);

            rsiList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_RsiEma_{request.RsiWindow}_{request.EmaWindow}.csv",
                rsi.Where(ma => ma.Signal != Signal.None)));

            rsiList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_RsiEma_{request.RsiWindow}_{request.EmaWindow}_Simulation.csv", tradingSim.Result));

            rsiList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_RsiEma_{request.RsiWindow}_{request.EmaWindow}_Summary.csv", new[] { tradingSim.Summary }));
        }

        if (!rsiList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(Results.File(rsiList.GetZipFromFileData(),
            "application/octet-stream", "RsiEma.zip"));
    }
}

public record RsiEmaRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int RsiWindow { get; set; }
    public int EmaWindow { get; set; }
    public double? RsiLimit { get; set; }
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public double? RiskReward { get; set; }
    public int? TradeRisk { get; set; }
}