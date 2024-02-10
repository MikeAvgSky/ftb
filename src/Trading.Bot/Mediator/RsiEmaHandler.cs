namespace Trading.Bot.Mediator;

public class RsiEmaRequestHandler : IRequestHandler<RsiEmaRequest, IResult>
{
    public Task<IResult> Handle(RsiEmaRequest request, CancellationToken cancellationToken)
    {
        var rsiList = new List<FileData<IEnumerable<RsiResult>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var rsi = candles.CalcRsi(request.RsiWindow ?? 14).ToArray();

            var ema = candles.Select(c => c.Mid_C).CalcEma(request.EmaWindow ?? 200).ToArray();

            rsiList.Add(new FileData<IEnumerable<RsiResult>>(
                $"{instrument}_{granularity}_RSI_EMA.csv",
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