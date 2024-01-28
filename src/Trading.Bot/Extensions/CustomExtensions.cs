namespace Trading.Bot.Extensions;

public static class CustomExtensions
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

                while(index < list.Count)
                {
                    if (i == index) index++;

                    if (index == list.Count) break;

                    yield return $"{list[i]}_{list[index]}";

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
            using (var csv = new CsvWriter(writer, CultureInfo.GetCultureInfo("en-US")))
            {
                csv.WriteRecords(sequence);
            }
        }

        return memoryStream.ToArray();
    }

    public static byte[] GetCsvBytesFromDictionary(this List<Dictionary<string, object>> sequence)
    {
        using var memoryStream = new MemoryStream();

        using (var writer = new StreamWriter(memoryStream))
        {
            using (var csv = new CsvWriter(writer, CultureInfo.GetCultureInfo("en-US")))
            {
                var headings = new List<string>(sequence.First().Keys);

                foreach (var heading in headings)
                {
                    csv.WriteField(heading);
                }

                csv.NextRecord();

                foreach (var item in sequence)
                {
                    foreach (var heading in headings)
                    {
                        csv.WriteField(item[heading]);
                    }

                    csv.NextRecord();
                }
            }
        }

        return memoryStream.ToArray();
    }

    public static byte[] GetCsvBytesFromDataFrame(this DataFrame dataFrame)
    {
        var data = new List<Dictionary<string, object>>();

        foreach (var row in dataFrame.Rows)
        {
            var dict = new Dictionary<string, object>();

            foreach (var column in dataFrame.Columns)
            {
                dict.Add(column.Name, row[column.Name]);
            }

            data.Add(dict);
        }

        return data.GetCsvBytesFromDictionary();
    }

    public static byte[] GetZipFromFileData(this IEnumerable<FileData<IEnumerable<Candle>>> files)
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

    public static IEnumerable<double> SimpleMovingAverage(this IEnumerable<double> source, int window)
    {
        var queue = new Queue<double>(window);

        foreach (var d in source)
        {
            if (queue.Count == window)
            {
                queue.Dequeue();
            }

            queue.Enqueue(d);

            yield return queue.Average();
        }
    }

    public static IEnumerable<double> CumulativeMovingAverage(this IEnumerable<double> source, int window)
    {
        double total = 0;

        var count = 0;

        foreach (var d in source)
        {
            count++;

            total += d;

            yield return total / count;
        }
    }
}