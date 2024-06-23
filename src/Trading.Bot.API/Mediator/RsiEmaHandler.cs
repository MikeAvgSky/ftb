namespace Trading.Bot.API.Mediator;

public class RsiEmaRequestHandler : IRequestHandler<RsiEmaRequest, IResult>
{
    public Task<IResult> Handle(RsiEmaRequest request, CancellationToken cancellationToken)
    {
        var rsiList = new List<FileData<IEnumerable<object>>>();

        var rsiLimit = request.RsiLimit ?? 50;

        var maxSpread = request.MaxSpread ?? 0.0004;

        var minGain = request.MinGain ?? 0.0006;

        var riskReward = request.RiskReward ?? 1;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var rsi = candles.CalcRsiEma(request.RsiWindow, request.EmaWindow, rsiLimit, maxSpread, minGain, riskReward);

            var tradingSim = TradeResult.SimulateTrade(rsi.Cast<IndicatorBase>().ToArray());

            rsiList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_RSI_{request.RsiWindow}_EMA_{request.EmaWindow}.csv",
                request.ShowTradesOnly ? rsi.Where(ma => ma.Signal != Signal.None) : rsi));

            rsiList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_RSI_{request.RsiWindow}_EMA_{request.EmaWindow}_SIM.csv", tradingSim));
        }

        if (!rsiList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(request.Download
            ? Results.File(rsiList.GetZipFromFileData(),
                "application/octet-stream", "rsi_ema.zip")
            : Results.Ok(rsiList.Select(l => l.Value)));
    }
}

public record RsiEmaRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int RsiWindow { get; set; }
    public int EmaWindow { get; set; }
    public int? RsiLimit { get; set; }
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public int? RiskReward { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}