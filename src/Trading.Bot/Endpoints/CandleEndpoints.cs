namespace Trading.Bot.Endpoints;

public static class CandleEndpoints
{
    public static void MapCandleEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/candles/{instrument}", GetCandles);
    }

    private static async Task<IResult> GetCandles(OandaApiService apiService, string instrument, string fromDate, string toDate,
        string granularity = "H1", string price = "MBA", int count = 10)
    {
        try
        {
            DateTime.TryParse(fromDate, out var _fromDate);

            DateTime.TryParse(toDate, out var _toDate);

            var apiResponse = await apiService.GetCandles(instrument, granularity, price, count, _fromDate, _toDate);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };

                var candles = apiResponse.Value.Candles.Where(c => c.Complete).Select(MapToCandle);

                var bytes = JsonSerializer.SerializeToUtf8Bytes(candles, options);

                return Results.File(bytes, "application/json", $"{instrument}_{granularity}_Candles.json");
            }

            return Results.Empty;
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static Candle MapToCandle(CandleData candleData)
    {
        return new Candle(candleData);
    }
}