namespace Trading.Bot.Mediator;

public class CalculateBollingerBandsHandler : IRequestHandler<CalculateBollingerBandsRequest, IResult>
{
    private readonly OandaApiService _apiService;

    public CalculateBollingerBandsHandler(OandaApiService apiService)
    {
        _apiService = apiService;
    }

    public Task<IResult> Handle(CalculateBollingerBandsRequest request, CancellationToken cancellationToken)
    {
        var bollingerBandsList = new List<FileData<IEnumerable<BollingerBands>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToList();

            var standardDeviation = typicalPrice
                .MovingStandardDeviation(request.Window ?? 20, request.StandardDeviation ?? 2).ToList();

            var bollingerBandsAverage = typicalPrice.MovingAverage(request.Window ?? 20).ToList();

            var bollingerBands =
                CreateBollingerBands(candles, bollingerBandsAverage, standardDeviation, request.StandardDeviation ?? 2);

            bollingerBandsList.Add(new FileData<IEnumerable<BollingerBands>>(
                $"{instrument}_{granularity}_BB_{request.Window ?? 20}_{request.StandardDeviation ?? 2}.csv",
                request.ShowTradesOnly ? bollingerBands.Where(ma => ma.Trade != Trade.None) : bollingerBands));
        }

        if (!bollingerBandsList.Any()) return Task.FromResult(Results.Empty);

        return Task.FromResult(request.Download
            ? Results.File(bollingerBandsList.GetZipFromFileData(),
                "application/octet-stream", "ma.zip")
            : Results.Ok(bollingerBandsList.Select(l => l.Value)));
    }

    private static IEnumerable<BollingerBands> CreateBollingerBands(IEnumerable<Candle> candles,
        IReadOnlyList<double> bollingerBandsAverage, IReadOnlyList<double> standardDeviation, int std)
    {
        var bollingerBands = candles.Select(c => new BollingerBands(c)).ToList();

        for (var i = 0; i < bollingerBands.Count; i++)
        {
            bollingerBands[i].BollingerAverage = bollingerBandsAverage[i];

            bollingerBands[i].BollingerTop = bollingerBands[i].BollingerAverage + standardDeviation[i] * std;

            bollingerBands[i].BollingerBottom = bollingerBands[i].BollingerAverage - standardDeviation[i] * std;
        }

        return bollingerBands;
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