namespace Trading.Bot.Endpoints;

public static class CandleEndpoints
{
    public static void MapCandleEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/candles/{currencies}", GetCandles);

        builder.MapPost("api/candles/mac", CalculateMovingAverageCross);
    }

    private static async Task<IResult> GetCandles(OandaApiService apiService, string currencies, 
        string fromDate, string toDate, string granularity = "H1", string price = "MBA", int count = 10)
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

            return !candlesBag.Any()
                ? Results.Empty
                : Results.File(candlesBag.GetZipFromFileData(), 
                    "application/octet-stream", "candles.zip");
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static IResult CalculateMovingAverageCross(IFormFile file, int ma_s = 10, int ma_l = 20, MovingAverage ma = MovingAverage.Simple)
    {
        try
        {
            var candles = GetCandlesFromCsv(file);

            var stringData = ReadFile(file);

            var dataFrame = DataFrame.LoadCsvFromString(stringData);

            var windows = new[] { ma_s, ma_l };

            foreach (var window in windows)
            {
                var maValues = ma switch
                {
                    MovingAverage.Simple => candles.Select(c => c.Mid_C).SimpleMovingAverage(window),
                    MovingAverage.Cumulative => candles.Select(c => c.Mid_C).CumulativeMovingAverage(window),
                    _ => candles.Select(c => c.Mid_C).SimpleMovingAverage(window)
                };

                PrimitiveDataFrameColumn<double> col = new($"MA_{window}", maValues);

                dataFrame.Columns.Add(col);
            }

            dataFrame["Delta"] = dataFrame[$"MA_{ma_s}"].Subtract(dataFrame[$"MA_{ma_l}"]);

            return Results.File(dataFrame.GetCsvBytesFromDataFrame(), "text/csv", "candles_mac.csv");
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

    private static string ReadFile(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());

        using var ms = new MemoryStream();

        reader.BaseStream.CopyTo(ms);

        return Encoding.UTF8.GetString(ms.ToArray());
    }
}