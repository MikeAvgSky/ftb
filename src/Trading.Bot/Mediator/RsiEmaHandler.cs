namespace Trading.Bot.Mediator;

public class RsiEmaRequestHandler : IRequestHandler<RsiEmaRequest, IResult>
{
    public Task<IResult> Handle(RsiEmaRequest request, CancellationToken cancellationToken)
    {
        var rsiList = new List<FileData<IEnumerable<RsiEmaResult>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var rsiWindow = request.RsiWindow ?? 14;

            var emaWindow = request.EmaWindow ?? 200;

            var rsi = candles.CalcRsiEma(rsiWindow, emaWindow);

            rsiList.Add(new FileData<IEnumerable<RsiEmaResult>>(
                $"{instrument}_{granularity}_RSI_{rsiWindow}_EMA_{emaWindow}.csv",
                request.ShowTradesOnly ? rsi.Where(ma => ma.Signal != Signal.None) : rsi));
        }

        if (!rsiList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(request.Download
            ? Results.File(rsiList.GetZipFromFileData(),
                "application/octet-stream", "bb.zip")
            : Results.Ok(rsiList.Select(l => l.Value)));
    }
}

public record RsiEmaRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; }
    public int? RsiWindow { get; set; }
    public int? EmaWindow { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}