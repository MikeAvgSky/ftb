namespace Trading.Bot.Endpoints;

public static class StrategyEndpoints
{
    public static void MapStrategyEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("api/strategy/ma/{maShort}/{maLong}", CalculateMovingAverage);
        builder.MapPost("api/strategy/result/{strategy}", CalculateStrategyResult);
    }

    private static async Task<IResult> CalculateMovingAverage(ISender sender,
        [AsParameters] CalculateMovingAverageRequest request)
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

    private static async Task<IResult> CalculateStrategyResult(ISender sender,
        [AsParameters] CalculateStrategyResultRequest request)
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