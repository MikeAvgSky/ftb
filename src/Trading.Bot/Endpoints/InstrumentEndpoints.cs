namespace Trading.Bot.Endpoints;

public static class InstrumentEndpoints
{
    public static void MapInstrumentEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/instruments", GetInstrumentCollection);
    }

    private static async Task<IResult> GetInstrumentCollection(OandaApiService apiService, string instruments = default)
    {
        try
        {
            var instrumentList = (await apiService.GetInstruments(instruments)).ToList();

            if (!instrumentList.Any()) return Results.Empty;

            var options = new JsonSerializerOptions { WriteIndented = true };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(instrumentList, options);

            return Results.File(bytes, "application/json", "instruments.json");

        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }
}