namespace Trading.Bot.Mediator;

public sealed class InstrumentsHandler : IRequestHandler<InstrumentsRequest, IResult>
{
    private readonly OandaApiService _apiService;
    private readonly OandaStreamService _streamService;

    public InstrumentsHandler(OandaApiService apiService, OandaStreamService streamService)
    {
        _apiService = apiService;
        _streamService = streamService;
    }

    public async Task<IResult> Handle(InstrumentsRequest request, CancellationToken cancellationToken)
    {
        var instrumentList = (await _apiService.GetInstruments(request.Instruments)).ToList();

        await _streamService.StreamLivePrices(request.Instruments);

        if (!string.IsNullOrEmpty(request.Type))
        {
            instrumentList.RemoveAll(i => 
                !string.Equals(i.Type, request.Type, StringComparison.OrdinalIgnoreCase));
        }

        if (!instrumentList.Any()) return Results.Empty;

        return request.Download
            ? Results.File(instrumentList.GetCsvBytes(), 
                "text/csv", "instruments.csv")
            : Results.Ok(instrumentList);
    }
}

public record InstrumentsRequest : IHttpRequest
{
    public string Instruments { get; set; }
    public string Type { get; set; }
    public bool Download { get; set; }
}