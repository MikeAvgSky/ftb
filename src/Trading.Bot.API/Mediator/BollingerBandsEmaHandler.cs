namespace Trading.Bot.API.Mediator;

public class BollingerBandsEmaHandler : IRequestHandler<BollingerBandsEmaRequest, IResult>
{
    public Task<IResult> Handle(BollingerBandsEmaRequest request, CancellationToken cancellationToken)
    {
        var bollingerBandsList = new List<FileData<IEnumerable<object>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var maxSpread = request.MaxSpread ?? 0.0004;

            var minGain = request.MinGain ?? 0.0006;

            var minVolume = request.MinVolume ?? 100;

            var bollingerBands = candles.CalcBollingerBandsEma(request.Window, request.EmaWindow,
                request.StandardDeviation, maxSpread, minGain, minVolume);

            var tradingSim = TradeResult.SimulateTrade(bollingerBands.Cast<IndicatorBase>().ToArray());

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
            $"{instrument}_{granularity}_BB_EMA_{request.Window}_{request.EmaWindow}_{request.StandardDeviation}.csv",
            request.ShowTradesOnly ? bollingerBands.Where(ma => ma.Signal != Signal.None) : bollingerBands));

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_BB_EMA_{request.Window}_{request.EmaWindow}_{request.StandardDeviation}_SIM.csv", tradingSim));
        }

        if (!bollingerBandsList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(request.Download
            ? Results.File(bollingerBandsList.GetZipFromFileData(),
                "application/octet-stream", "bb.zip")
            : Results.Ok(bollingerBandsList.Select(l => l.Value)));
    }
}

public record BollingerBandsEmaRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int Window { get; set; }
    public int EmaWindow { get; set; }
    public double StandardDeviation { get; set; }
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public int? MinVolume { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}