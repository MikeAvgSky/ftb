namespace Trading.Bot.Endpoints;

public static class StrategyEndpoints
{
    public static void MapStrategyEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("api/strategy/mac", CalculateMovingAverage);
        builder.MapPost("api/strategy/bb", CalculateBollingerBands);
        builder.MapPost("api/strategy/results", CalculateStrategyResults);
    }

    private static async Task<IResult> CalculateMovingAverage(ISender sender,
        [AsParameters] MovingAverageCrossRequest crossRequest)
    {
        try
        {
            return await sender.Send(crossRequest);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<IResult> CalculateBollingerBands(ISender sender,
        [AsParameters] BollingerBandsRequest request)
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

    private static async Task<IResult> CalculateStrategyResults(ISender sender,
        [AsParameters] StrategyResultRequest request)
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