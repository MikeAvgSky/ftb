namespace Trading.Bot.Mediator;

public sealed class GetInstrumentsHandler : IRequestHandler<GetInstrumentsRequest, IResult>
{
    private readonly OandaApiService _apiService;

    public GetInstrumentsHandler(OandaApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IResult> Handle(GetInstrumentsRequest request, CancellationToken cancellationToken)
    {
        var instrumentList = (await _apiService.GetInstrumentsFromOanda(request.Instruments)).ToList();

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

public record GetInstrumentsRequest : IHttpRequest
{
    public string Instruments { get; set; }
    public string Type { get; set; }
    public bool Download { get; set; }
}