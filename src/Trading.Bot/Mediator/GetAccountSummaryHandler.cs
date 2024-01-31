namespace Trading.Bot.Mediator;

public sealed class GetAccountSummaryHandler : IRequestHandler<GetAccountSummaryRequest, IResult>
{
    private readonly OandaApiService _apiService;

    public GetAccountSummaryHandler(OandaApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IResult> Handle(GetAccountSummaryRequest request, CancellationToken cancellationToken)
    {
        var apiResponse = await _apiService.GetOandaAccountSummary();

        if (apiResponse.StatusCode == HttpStatusCode.OK)
        {
            var bytes = new List<AccountResponse> { apiResponse.Value }.GetCsvBytes();

            return request.Download
                ? Results.File(bytes, "text/csv", "instruments.csv")
                : Results.Ok(apiResponse.Value);
        }

        return Results.Empty;
    }
}

public record GetAccountSummaryRequest : IHttpRequest
{
    public bool Download { get; set; }
}