namespace Trading.Bot.Mediator;

public class MacdEmaHandler : IRequestHandler<MacdEmaRequest, IResult>
{
    public Task<IResult> Handle(MacdEmaRequest request, CancellationToken cancellationToken)
    {
        var macdEmaList = new List<FileData<IEnumerable<object>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var emaWindow = request.EmaWindow ?? 100;

            var macdEma = candles.CalcMacdEma(emaWindow);

            var tradingSim = TradeResult.SimulateTrade(macdEma.Cast<IndicatorBase>().ToArray());

            macdEmaList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_MACD_EMA_{emaWindow}.csv",
                request.ShowTradesOnly ? macdEma.Where(ma => ma.Signal != Signal.None) : macdEma));

            macdEmaList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_MACD_EMA_{emaWindow}_SIM.csv", tradingSim));
        }

        if (!macdEmaList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(request.Download
            ? Results.File(macdEmaList.GetZipFromFileData(),
                "application/octet-stream", "bb.zip")
            : Results.Ok(macdEmaList.Select(l => l.Value)));
    }
}

public record MacdEmaRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; }
    public int? EmaWindow { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}