namespace Trading.Bot.API.Mediator;

public class NextCandleHandler : IRequestHandler<NextCandleRequest, IResult>
{
    public Task<IResult> Handle(NextCandleRequest request, CancellationToken cancellationToken)
    {
        var macdEmaList = new List<FileData<IEnumerable<object>>>();

        var maxSpread = request.MaxSpread ?? 0.0004;

        var minGain = request.MinGain ?? 0.0006;

        var riskReward = request.RiskReward ?? 1;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var macdEma = candles.CalcNextCandle(0.001, maxSpread, minGain, riskReward);

            var tradingSim = TradeResult.SimulateTrade(macdEma.Cast<IndicatorBase>().ToArray());

            macdEmaList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_NEXT_CANDLE.csv",
                request.ShowTradesOnly ? macdEma.Where(ma => ma.Signal != Signal.None) : macdEma));

            macdEmaList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_NEXT_CANDLE_SIM.csv", tradingSim.Result));

            macdEmaList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_NEXT_CANDLE_Summary.csv", new[] { tradingSim.Summary }));
        }

        if (!macdEmaList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(Results.File(macdEmaList.GetZipFromFileData(),
            "application/octet-stream", "next_candle.zip"));
    }
}

public record NextCandleRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public double? RiskReward { get; set; }
    public bool ShowTradesOnly { get; set; }
}