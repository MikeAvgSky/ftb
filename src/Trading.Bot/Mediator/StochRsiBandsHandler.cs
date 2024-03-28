namespace Trading.Bot.Mediator;

public class StochRsiBandsHandler : IRequestHandler<StochRsiBandsRequest, IResult>
{
    public Task<IResult> Handle(StochRsiBandsRequest request, CancellationToken cancellationToken)
    {
        var bollingerBandsList = new List<FileData<IEnumerable<object>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var bbWindow = request.BbWindow ?? 20;

            var rsiWindow = request.RsiWindow ?? 13;

            var stdDev = request.StandardDeviation ?? 2;

            var maxSpread = request.MaxSpread ?? 0.0004;

            var minGain = request.MinGain ?? 0.0006;

            var riskReward = request.RiskReward ?? 1.5;

            var rsiBands = candles.CalcStochRsiBands(bbWindow, rsiWindow, stdDev, maxSpread, minGain, riskReward);

            var tradingSim = TradeResult.SimulateTrade(rsiBands.Cast<IndicatorBase>().ToArray());

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
            $"{instrument}_{granularity}_RSI_BB_{rsiWindow}_{bbWindow}_{stdDev}.csv",
            request.ShowTradesOnly ? rsiBands.Where(ma => ma.Signal != Signal.None) : rsiBands));

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_RSI_BB_{rsiWindow}_{bbWindow}_{stdDev}_SIM.csv", tradingSim));
        }

        if (!bollingerBandsList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(request.Download
            ? Results.File(bollingerBandsList.GetZipFromFileData(),
                "application/octet-stream", "rsi_bb.zip")
            : Results.Ok(bollingerBandsList.Select(l => l.Value)));
    }
}

public record StochRsiBandsRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; }
    public int? BbWindow { get; set; }
    public int? RsiWindow { get; set; }
    public double? StandardDeviation { get; set; }
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public double? RiskReward { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}