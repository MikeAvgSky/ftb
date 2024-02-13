namespace Trading.Bot.Mediator;

public class BollingerBandsHandler : IRequestHandler<BollingerBandsRequest, IResult>
{
    public Task<IResult> Handle(BollingerBandsRequest request, CancellationToken cancellationToken)
    {
        var bollingerBandsList = new List<FileData<IEnumerable<object>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var window = request.Window ?? 20;

            var stdDev = request.StandardDeviation ?? 2;

            var bollingerBands = candles.CalcBollingerBands(window, stdDev);

            var tradingSim = TradeResult.SimulateTrade(bollingerBands.Cast<IndicatorBase>().ToArray());

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
            $"{instrument}_{granularity}_BB_{window}_{stdDev}.csv",
            request.ShowTradesOnly ? bollingerBands.Where(ma => ma.Signal != Signal.None) : bollingerBands));

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_BB_{window}_{stdDev}_SIM.csv", tradingSim));
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
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}