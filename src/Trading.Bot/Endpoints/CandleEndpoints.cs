namespace Trading.Bot.Endpoints;

public static class CandleEndpoints
{
    public static void MapCandleEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("api/candles/{currencies}", GetCandles);
    }

    private static async Task<IResult> GetCandles(OandaApiService apiService, string currencies, 
        string fromDate, string toDate, string granularity = "H1", string price = "MBA", int count = 10)
    {
        try
        {
            if (!currencies.Contains(',')) return Results.BadRequest("Please provide comma separated currencies");

            var currencyList = currencies.Split(',', StringSplitOptions.RemoveEmptyEntries);

            DateTime.TryParse(fromDate, out var _fromDate);

            DateTime.TryParse(toDate, out var _toDate);

            var instruments = currencyList.GetAllCombinations().ToList();

            var candleResponses = new ConcurrentBag<CandleResponse>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 3
            };

            await Parallel.ForEachAsync(instruments, parallelOptions, async (instrument, _) =>
            {
                var apiResponse = await apiService.GetCandles(instrument, granularity, price, count, _fromDate, _toDate);

                if (apiResponse.StatusCode == HttpStatusCode.OK) candleResponses.Add(apiResponse.Value);
            });

            if (!candleResponses.Any()) return Results.Empty;

            var zipFile = await GetZippedFile(candleResponses, granularity);

            return Results.File(zipFile, "application/octet-stream", "candles.zip");
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    private static async Task<byte[]> GetZippedFile(ConcurrentBag<CandleResponse> candleResponses, string granularity)
    {
        var zipFileList = candleResponses.ToDictionary(cr =>
            $"{cr.Instrument}_{granularity}.csv", GetCsvCandleBytes);

        using var memoryStream = new MemoryStream();

        using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var zipFile in zipFileList)
            {
                var zipEntry = zipArchive.CreateEntry(zipFile.Key, CompressionLevel.Optimal);

                await using var entryStream = zipEntry.Open();

                var fileStream = new MemoryStream(zipFile.Value);

                await fileStream.CopyToAsync(entryStream);
            }
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream.ToArray();
    }

    private static byte[] GetCsvCandleBytes(CandleResponse cr)
    {
        var candles = cr.Candles.Where(c => c.Complete).Select(MapToCandle).ToList();

        using var memoryStream = new MemoryStream();

        using (var writer = new StreamWriter(memoryStream))
        {
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecordsAsync(candles);
            }
        }

        return memoryStream.ToArray();
    }

    private static Candle MapToCandle(CandleData candleData)
    {
        return new Candle(candleData);
    }
}