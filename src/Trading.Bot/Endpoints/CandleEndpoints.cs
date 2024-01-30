namespace Trading.Bot.Endpoints;

public static class CandleEndpoints
{
    public static void MapCandleEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/candles/{currencies}", GetCandles);

        builder.MapPost("api/candles/moving_average_cross", CalculateMovingAverageCross);
    }

    private static async Task<IResult> GetCandles(ISender sender, 
        [AsParameters] GetCandlesRequest request)
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

    private static async Task<IResult> CalculateMovingAverageCross(ISender sender, 
        [AsParameters] CalculateMovingAverageCrossRequest request)
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