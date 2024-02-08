namespace Trading.Bot.Endpoints;

public static class InstrumentEndpoints
{
    public static void MapInstrumentEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/instruments", GetInstrumentCollection);
    }

    private static async Task<IResult> GetInstrumentCollection(ISender sender,
        [AsParameters] InstrumentsRequest request)
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