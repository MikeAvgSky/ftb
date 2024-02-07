namespace Trading.Bot.Mediator;

public class CalculateBollingerBandsHandler : IRequestHandler<CalculateBollingerBandsRequest, IResult>
{
    public Task<IResult> Handle(CalculateBollingerBandsRequest request, CancellationToken cancellationToken)
    {
        var bollingerBandsList = new List<FileData<IEnumerable<BollingerBands>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var bollingerBands = BollingerBands.ProcessCandles(candles, new TradeSettings());

                bollingerBandsList.Add(new FileData<IEnumerable<BollingerBands>>(
                $"{instrument}_{granularity}_BB_{request.Window ?? 20}_{request.StandardDeviation ?? 2}.csv",
                request.ShowTradesOnly ? bollingerBands.Where(ma => ma.Signal != Signal.None) : bollingerBands));
        }

        if (!bollingerBandsList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(request.Download
            ? Results.File(bollingerBandsList.GetZipFromFileData(),
                "application/octet-stream", "bb.zip")
            : Results.Ok(bollingerBandsList.Select(l => l.Value)));
    }
}

public record CalculateBollingerBandsRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; }
    public int? Window { get; set; }
    public int? StandardDeviation { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}