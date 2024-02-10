namespace Trading.Bot.Endpoints;

public static class SimulationEndpoints
{
    public static void MapSimulationEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("api/simulation/ma_cross", SimulateMovingAverageCross);
        builder.MapPost("api/simulation/bb", SimulateBollingerBands);
        builder.MapPost("api/simulation/rsi_ema", SimulateRsiEma);
        builder.MapPost("api/simulation/results", CalculateSimulationResults);
    }

    private static async Task<IResult> SimulateMovingAverageCross(ISender sender,
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
     
    private static async Task<IResult> SimulateBollingerBands(ISender sender,
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

    private static async Task<IResult> SimulateRsiEma(ISender sender,
        [AsParameters] RsiEmaRequest request)
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

    private static async Task<IResult> CalculateSimulationResults(ISender sender,
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