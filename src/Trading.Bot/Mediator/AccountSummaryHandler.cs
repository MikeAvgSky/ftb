namespace Trading.Bot.Mediator;

public sealed class AccountSummaryHandler : IRequestHandler<AccountSummaryRequest, IResult>
{
    private readonly OandaApiService _apiService;

    public AccountSummaryHandler(OandaApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<IResult> Handle(AccountSummaryRequest request, CancellationToken cancellationToken)
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

public record AccountSummaryRequest : IHttpRequest
{
    public bool Download { get; set; }
}