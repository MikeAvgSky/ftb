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
            var apiResponse = await apiService.GetInstruments(instruments);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };

                var bytes = JsonSerializer.SerializeToUtf8Bytes(apiResponse.Value.Select(MapToInstrument), options);

                return Results.File(bytes, "application/json", "instruments.json");
            }

            return Results.Empty;
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static Instrument MapToInstrument(InstrumentResponse ir)
    {
        return new Instrument(ir.Name, ir.Type, ir.DisplayName, ir.PipLocation, ir.TradeUnitsPrecision, ir.MarginRate);
    }
}