namespace Trading.Bot.API.Mediator;

public class BollingerBandsHandler : IRequestHandler<BollingerBandsRequest, IResult>
{
    public Task<IResult> Handle(BollingerBandsRequest request, CancellationToken cancellationToken)
    {
        var bollingerBandsList = new List<FileData<IEnumerable<object>>>();

        var maxSpread = request.MaxSpread ?? 0.0003;

        var minGain = request.MinGain ?? 0.002;

        var riskReward = request.RiskReward ?? 1;

        var tradeRisk = request.TradeRisk ?? 10;

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var bollingerBands = candles.CalcMeanReversion(request.Window, request.StandardDeviation,
                30, 70, maxSpread, minGain, riskReward);

            var tradingSim = TradeResult.SimulateTrade(bollingerBands.Cast<IndicatorBase>().ToArray(), tradeRisk, riskReward);

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
            $"{instrument}_{granularity}_MeanReversion_{request.Window}_{request.StandardDeviation}_{request.MinGain}.csv",
            bollingerBands.Where(ma => ma.Signal != Signal.None)));

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_MeanReversion_{request.Window}_{request.StandardDeviation}_{request.MinGain}_Simulation.csv", tradingSim.Result));

            bollingerBandsList.Add(new FileData<IEnumerable<object>>(
                $"{instrument}_{granularity}_MeanReversion_{request.Window}_{request.StandardDeviation}_Summary.csv", new[] { tradingSim.Summary }));
        }

        if (!bollingerBandsList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(Results.File(bollingerBandsList.GetZipFromFileData(),
            "application/octet-stream", "_MeanReversion_.zip"));
    }
}

public record BollingerBandsRequest : IHttpRequest
{
    public IFormFileCollection Files { get; set; } = new FormFileCollection();
    public int Window { get; set; }
    public double StandardDeviation { get; set; }
    public double? MaxSpread { get; set; }
    public double? MinGain { get; set; }
    public double? RiskReward { get; set; }
    public int? TradeRisk { get; set; }
}