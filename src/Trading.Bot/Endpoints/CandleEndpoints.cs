namespace Trading.Bot.Endpoints;

public static class CandleEndpoints
{
    public static void MapCandleEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/candles/{currencies}", GetCandles);

        builder.MapPost("api/candles/moving_average_cross", CalculateMovingAverageCross);
    }

    private static async Task<IResult> GetCandles(OandaApiService apiService, string currencies, string fromDate, string toDate,
        string granularity = "H1", string price = "MBA", int count = 10, bool downLoadable = true)
    {
        try
        {
            if (!currencies.Contains(','))
            {
                return Results.BadRequest("Please provide comma separated currencies");
            }
            
            var currencyList = currencies.Split(',', StringSplitOptions.RemoveEmptyEntries);

            DateTime.TryParse(fromDate, out var _fromDate);

            DateTime.TryParse(toDate, out var _toDate);

            var instruments = currencyList.GetAllCombinations();

            var candlesBag = new ConcurrentBag<FileData<IEnumerable<Candle>>>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 3
            };

            await Parallel.ForEachAsync(instruments, parallelOptions, async (instrument, _) =>
            {
                var candles = (await apiService.GetCandles(instrument, granularity, price, count, _fromDate, _toDate)).ToList();

                if (candles.Any())
                {
                    candlesBag.Add(new FileData<IEnumerable<Candle>>($"{instrument}_{granularity}.csv", candles));
                }
            });

            if (!candlesBag.Any())
            {
                return Results.Empty;
            }

            return downLoadable
                ? Results.File(candlesBag.GetZipFromFileData(),
                    "application/octet-stream", "candles.zip")
                : Results.Ok(candlesBag);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static IResult CalculateMovingAverageCross(IFormFile file, int ma_s = 10, int ma_l = 20, bool downLoadable = true)
    {
        try
        {
            var candles = GetCandlesFromCsv(file);

            if (!candles.Any()) return Results.Empty;

            var movingAvgCross = candles.Select(c => new MovingAverageCross(c)).ToList();

            var maShort = candles.Select(c => c.Mid_C).SimpleMovingAverage(ma_s).ToList();

            var maLong = candles.Select(c => c.Mid_C).SimpleMovingAverage(ma_l).ToList();

            for (var i = 0; i < movingAvgCross.Count; i++)
            {
                movingAvgCross[i].MaShort = maShort[i];
                movingAvgCross[i].MaLong = maLong[i];
                movingAvgCross[i].Delta = maShort[i] - maLong[i];
                movingAvgCross[i].DeltaPrev = i > 0 ? movingAvgCross[i - 1].Delta : 0;
                movingAvgCross[i].Trade = movingAvgCross[i].Delta switch
                {
                    >= 0 when movingAvgCross[i].DeltaPrev < 0 => Trade.Buy,
                    < 0 when movingAvgCross[i].DeltaPrev >= 0 => Trade.Sell,
                    _ => Trade.None
                };
                movingAvgCross[i].Diff = i < movingAvgCross.Count - 1
                    ? movingAvgCross[i + 1].Candle.Mid_C - movingAvgCross[i].Candle.Mid_C
                    : movingAvgCross[i].Candle.Mid_C;
            }

            return downLoadable
                ? Results.File(movingAvgCross.Where(m => m.Trade != Trade.None).GetCsvBytes(),
                    "text/csv", "candles_moving_average_cross.csv")
                : Results.Ok(candles);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static List<Candle> GetCandlesFromCsv(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());

        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        return csv.GetRecords<Candle>().ToList();
    }
}