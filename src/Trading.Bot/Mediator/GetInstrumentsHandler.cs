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

        return !instrumentList.Any()
            ? Results.Empty
            : Results.File(instrumentList.GetCsvBytes(), "text/csv", "instruments.csv");
    }
}

public record GetInstrumentsRequest : IHttpRequest
{
    public string Instruments { get; set; }
}