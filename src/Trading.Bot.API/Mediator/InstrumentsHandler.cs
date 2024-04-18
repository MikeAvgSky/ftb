namespace Trading.Bot.API.Mediator;

public sealed class InstrumentsHandler : IRequestHandler<InstrumentsRequest, IResult>
{
    private readonly OandaApiService _apiService;

    public InstrumentsHandler(OandaApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IResult> Handle(InstrumentsRequest request, CancellationToken cancellationToken)
    {
        var instrumentList = (await _apiService.GetInstruments(request.Instruments)).ToList();

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
    public string Instruments { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Download { get; set; }
}