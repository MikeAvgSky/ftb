namespace Trading.Bot.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/account", GetAccountSummary);
    }

    private static async Task<IResult> GetAccountSummary(OandaApiService apiService)
    {
        try
        {
            var apiResponse = await apiService.GetAccountSummary();

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };

                var bytes = JsonSerializer.SerializeToUtf8Bytes(apiResponse.Value, options);

                return Results.File(bytes, "application/json", "account.json");
            }

            return Results.Empty;
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }
}