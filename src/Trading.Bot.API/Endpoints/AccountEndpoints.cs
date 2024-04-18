namespace Trading.Bot.API.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/account", GetAccountSummary).DisableAntiforgery();
    }

    private static async Task<IResult> GetAccountSummary(ISender sender,
        [AsParameters] AccountSummaryRequest request)
    {
        try
        {
            return await sender.Send(request);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }
}