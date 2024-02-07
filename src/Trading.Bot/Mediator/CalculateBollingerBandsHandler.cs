namespace Trading.Bot.Mediator;

public class CalculateBollingerBandsHandler : IRequestHandler<CalculateBollingerBandsRequest, IResult>
{
    private readonly OandaApiService _apiService;

    public CalculateBollingerBandsHandler(OandaApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IResult> Handle(CalculateBollingerBandsRequest request, CancellationToken cancellationToken)
    {
        var bollingerBandsList = new List<FileData<IEnumerable<BollingerBands>>>();

        foreach (var file in request.Files)
        {
            var candles = file.GetObjectFromCsv<Candle>();

            if (!candles.Any()) continue;

            var instrument = file.FileName[..file.FileName.LastIndexOf('_')];

            var granularity = file.FileName[(file.FileName.LastIndexOf('_') + 1)..file.FileName.IndexOf('.')];

            var instrumentInfo = (await _apiService.GetInstrumentsFromOanda(instrument)).First();

            var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToList();

            var standardDeviation = typicalPrice
                .MovingStandardDeviation(request.Window ?? 20, request.StandardDeviation ?? 2).ToList();

            var bollingerBandsAverage = typicalPrice.MovingAverage(request.Window ?? 20).ToList();

            var bollingerBands =
                CreateBollingerBands(candles, instrumentInfo, bollingerBandsAverage, standardDeviation,
                    request.StandardDeviation ?? 2);

            bollingerBandsList.Add(new FileData<IEnumerable<BollingerBands>>(
                $"{instrument}_{granularity}_BB_{request.Window ?? 20}_{request.StandardDeviation ?? 2}.csv",
                request.ShowTradesOnly ? bollingerBands.Where(ma => ma.Trade != Signal.None) : bollingerBands));
        }

        if (!bollingerBandsList.Any()) return Results.Empty;

        return request.Download
            ? Results.File(bollingerBandsList.GetZipFromFileData(),
                "application/octet-stream", "bb.zip")
            : Results.Ok(bollingerBandsList.Select(l => l.Value));
    }

    private static IEnumerable<BollingerBands> CreateBollingerBands(IEnumerable<Candle> candles, Instrument instrumentInfo,
        IReadOnlyList<double> bollingerBandsAverage, IReadOnlyList<double> standardDeviation, int std)
    {
        var bollingerBands = candles.Select(c => new BollingerBands(c)).ToList();

        var lastTrade = Signal.None;

        var cumGain = 0.0;

        for (var i = 0; i < bollingerBands.Count; i++)
        {
            bollingerBands[i].BollingerAverage = bollingerBandsAverage[i];

            bollingerBands[i].BollingerTop = bollingerBands[i].BollingerAverage + standardDeviation[i] * std;

            bollingerBands[i].BollingerBottom = bollingerBands[i].BollingerAverage - standardDeviation[i] * std;

            bollingerBands[i].Trade = bollingerBands[i].Candle.Mid_C switch
            {
                var mid when mid < bollingerBands[i].BollingerBottom && lastTrade is not Signal.Buy => Signal.Buy,
                var mid when mid > bollingerBands[i].BollingerTop && lastTrade is not Signal.Sell => Signal.Sell,
                _ => Signal.None
            };

            if (bollingerBands[i].Trade is not Signal.None) lastTrade = bollingerBands[i].Trade;

            bollingerBands[i].Diff = i < bollingerBands.Count - 1
                ? bollingerBands[i + 1].Candle.Mid_C - bollingerBands[i].Candle.Mid_C
                : bollingerBands[i].Candle.Mid_C;

            bollingerBands[i].Gain = bollingerBands[i].Diff / instrumentInfo.PipLocation *
                                     bollingerBands[i].Trade.GetTradeValue();

            cumGain += bollingerBands[i].Gain;

            bollingerBands[i].CumGain = cumGain;
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