namespace Trading.Bot.Extensions;

public static class MiscellaneousExtensions
{
    public static IEnumerable<string> GetAllCombinations(this IEnumerable<string> sequence)
    {
        if (sequence == null)
        {
            yield break;
        }

        var list = sequence.ToList();

        if (!list.Any())
        {
            yield return string.Empty;
        }
        else
        {
            for (var i = 0; i < list.Count; i++)
            {
                var index = 0;

                while (index < list.Count)
                {
                    if (i == index) index++;

                    if (index == list.Count) break;

                    yield return $"{list[i]}_{list[index]}";

                    index++;
                }
            }
        }
    }

    public static IEnumerable<Tuple<int, int>> GetAllWindowCombinations(this IEnumerable<int> sequence)
    {
        if (sequence == null)
        {
            yield break;
        }

        var list = sequence.ToList();

        if (!list.Any())
        {
            yield return Tuple.Create(0, 0);
        }
        else
        {
            for (var i = 0; i < list.Count; i++)
            {
                var index = 0;

                while (index < list.Count)
                {
                    if (i == index) index++;

                    if (index == list.Count) break;

                    if (list[i] < list[index])
                    {
                        yield return Tuple.Create(list[i], list[index]);
                    }

                    index++;
                }
            }
        }
    }

    public static byte[] GetCsvBytes<T>(this IEnumerable<T> sequence)
    {
        using var memoryStream = new MemoryStream();

        using (var writer = new StreamWriter(memoryStream))
        {
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                var options = new TypeConverterOptions
                {
                    Formats = new[]
                    {
                        "o"
                    }
                };

                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);

                csv.WriteRecords(sequence);
            }
        }

        return memoryStream.ToArray();
    }

    public static byte[] GetZipFromFileData<T>(this IEnumerable<FileData<IEnumerable<T>>> files)
    {
        using var memoryStream = new MemoryStream();

        using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var file in files)
            {
                var zipEntry = zipArchive.CreateEntry(file.FileName, CompressionLevel.Optimal);

                using var entryStream = zipEntry.Open();

                var fileStream = new MemoryStream(file.Value.GetCsvBytes());

                fileStream.CopyTo(entryStream);
            }
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream.ToArray();
    }

    public static T[] GetObjectFromCsv<T>(this IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());

        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        return csv.GetRecords<T>().ToArray();
    }

    public static DateTime RoundDown(this DateTime time, TimeSpan candleSpan)
    {
        if (candleSpan.Days != 0)
        {
            return new DateTime(time.Year, time.Month, time.Day - time.Day % candleSpan.Days,
                0, 0, 0);
        }

        if (candleSpan.Hours != 0)
        {
            return new DateTime(time.Year, time.Month, time.Day,
                time.Hour - time.Hour % candleSpan.Hours, 0, 0);
        }

        if (candleSpan.Minutes != 0)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour,
                time.Minute - time.Minute % candleSpan.Minutes, 0);
        }

        return time;
    }

    public static bool IsSuccessStatusCode(this HttpStatusCode statusCode)
    {
        return (int)statusCode >= 200 && (int)statusCode <= 299;
    }

    public static double CalcTakeProfit(this Candle candle, IndicatorBase result)
    {
        return result.Signal switch
        {
            Signal.Buy => candle.Mid_C + result.Gain,
            Signal.Sell => candle.Mid_C - result.Gain,
            _ => 0.0
        };
    }

    public static double CalcStopLoss(this Candle candle, IndicatorBase result, double riskReward)
    {
        return result.Signal switch
        {
            Signal.Buy => candle.Mid_C - result.Gain / riskReward,
            Signal.Sell => candle.Mid_C + result.Gain / riskReward,
            _ => 0.0
        };
    }
}