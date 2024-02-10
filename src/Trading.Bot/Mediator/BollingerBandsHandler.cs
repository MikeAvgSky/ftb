namespace Trading.Bot.Mediator;

public class BollingerBandsHandler : IRequestHandler<BollingerBandsRequest, IResult>
{
    public Task<IResult> Handle(BollingerBandsRequest request, CancellationToken cancellationToken)
    {
        var bollingerBandsList = new List<FileData<IEnumerable<BollingerBandsResult>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var bollingerBands = candles.CalcBollingerBands(request.Window ?? 20, request.StandardDeviation ?? 2, request.RiskReward ?? 1.5);

            bollingerBandsList.Add(new FileData<IEnumerable<BollingerBandsResult>>(
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

public record BollingerBandsRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; }
    public int? Window { get; set; }
    public int? StandardDeviation { get; set; }
    public double? RiskReward { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}