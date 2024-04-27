namespace Trading.Bot.API.Mediator;

public class RsiBollingerBandsHandler : IRequestHandler<RsiBollingerBandsRequest, IResult>
{
    public Task<IResult> Handle(RsiBollingerBandsRequest request, CancellationToken cancellationToken)
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

            var rsiBands = candles.CalcRsiBollingerBands(request.BbWindow, request.RsiWindow, request.StandardDeviation,
                maxSpread, minGain, minVolume);

            var tradingSim = TradeResult.SimulateTrade(rsiBands.Cast<IndicatorBase>().ToArray());

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
            $"{instrument}_{granularity}_RSI_BB_{request.RsiWindow}_{request.BbWindow}_{request.StandardDeviation}.csv",
            request.ShowTradesOnly ? rsiBands.Where(ma => ma.Signal != Signal.None) : rsiBands));

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_RSI_BB_{request.RsiWindow}_{request.BbWindow}_{request.StandardDeviation}_SIM.csv", tradingSim));
        }

        if (!bollingerBandsList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(request.Download
            ? Results.File(bollingerBandsList.GetZipFromFileData(),
                "application/octet-stream", "rsi_bb.zip")
            : Results.Ok(bollingerBandsList.Select(l => l.Value)));
    }
}

public record RsiBollingerBandsRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int BbWindow { get; set; }
    public int RsiWindow { get; set; }
    public double StandardDeviation { get; set; }
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public int? MinVolume { get; set; }
    public bool Download { get; set; }
    public bool ShowTradesOnly { get; set; }
}