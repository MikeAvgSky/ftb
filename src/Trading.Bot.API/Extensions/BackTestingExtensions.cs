namespace Trading.Bot.API.Extensions;

public static class BackTestingExtensions
{
    public static IEnumerable<string> GetAllCombinations(this IEnumerable<string> sequence)
    {
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

    public static IEnumerable<FileData<IEnumerable<object>>> GetFileData(this IndicatorResult[] indicator, string fileName, int tradeRisk, double riskReward, bool trailingStop = false)
    {
        var fileData = new List<FileData<IEnumerable<object>>>();

        var tradingSim = SimulateTrade(indicator.Cast<IndicatorBase>().ToArray(), tradeRisk, riskReward, trailingStop);

        fileData.Add(new FileData<IEnumerable<object>>(
            $"{fileName}.csv", indicator.Where(ma => ma.Signal != Signal.None)));

        fileData.Add(new FileData<IEnumerable<object>>(
            $"{fileName}_Simulation.csv", tradingSim.Result));

        fileData.Add(new FileData<IEnumerable<object>>(
            $"{fileName}_Summary.csv", new[] { tradingSim.Summary }));

        return fileData;
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

    private static (TradeResult[] Result, SimulationSummary Summary) SimulateTrade(IndicatorBase[] indicators, int tradeRisk, double riskReward, bool trailingStop)
    {
        var length = indicators.Length;

        var openTrades = new List<TradeResult>();

        var closedTrades = new List<TradeResult>();

        for (var i = 0; i < length; i++)
        {
            if (indicators[i].Signal != Signal.None && !openTrades.Any())
            {
                openTrades.Add(new TradeResult
                {
                    Running = true,
                    StartIndex = i,
                    StartPrice = indicators[i].Signal == Signal.Buy
                        ? indicators[i].Candle.Ask_C
                        : indicators[i].Candle.Bid_C,
                    TriggerPrice = indicators[i].Signal == Signal.Buy
                        ? indicators[i].Candle.Ask_C
                        : indicators[i].Candle.Bid_C,
                    Signal = indicators[i].Signal,
                    TakeProfit = indicators[i].TakeProfit,
                    StopLoss = indicators[i].StopLoss,
                    StartTime = indicators[i].Candle.Time,
                    EndTime = indicators[i].Candle.Time,
                    Result = 0.0
                });

                continue;
            }

            foreach (var trade in openTrades)
            {
                UpdateTrade(trade, indicators[i], trailingStop);

                if (ShouldUpdateStopLoss(trailingStop, trade)) UpdateStopLoss(trade, indicators[i]);

                if (trade.Running) continue;

                closedTrades.Add(trade);
            }

            openTrades.RemoveAll(ot => !ot.Running);
        }

        var summary = new SimulationSummary
        {
            Days = indicators.Last().Candle.Time.Subtract(indicators.First().Candle.Time).Days,
            Candles = indicators.Length,
            Trades = closedTrades.Count,
            Wins = closedTrades.Count(t => t.Result > 0),
            Losses = closedTrades.Count(t => t.Result < 0),
            Unknown = closedTrades.Count(t => t.Result == 0),
            TradeRisk = tradeRisk
        };

        summary.WinRate = Math.Round((double)summary.Wins * 100 / (summary.Trades - summary.Unknown), 2);

        var buyWins = closedTrades.Count(t => t.Result > 0 && t.Signal == Signal.Buy);

        var buyTrades = closedTrades.Count(t => t.Result != 0 && t.Signal == Signal.Buy);

        summary.BuyWinRate = Math.Round((double)buyWins * 100 / buyTrades, 2);

        var sellWins = closedTrades.Count(t => t.Result > 0 && t.Signal == Signal.Sell);

        var sellTrades = closedTrades.Count(t => t.Result != 0 && t.Signal == Signal.Sell);

        summary.SellWinRate = Math.Round((double)sellWins * 100 / sellTrades, 2);

        summary.Winnings = trailingStop ? double.NaN : Math.Round(summary.Wins * (tradeRisk * riskReward) - summary.Losses * tradeRisk, 2);

        return (closedTrades.ToArray(), summary);
    }

    private static bool ShouldUpdateStopLoss(bool trailingStop, TradeResult trade) => trailingStop && trade.Running && trade.UnrealisedPL > 0;

    private static void UpdateTrade(TradeResult trade, IndicatorBase indicator, bool trailingStop)
    {
        if (trade.Signal == Signal.Buy)
        {
            if (indicator.Candle.Bid_H >= trade.TakeProfit && indicator.Candle.Bid_L > trade.StopLoss)
            {
                CloseTrade(trade, 1, indicator.Candle.Time, indicator.Candle.Bid_H);
            }

            if (indicator.Candle.Bid_L <= trade.StopLoss && indicator.Candle.Bid_H < trade.TakeProfit)
            {
                CloseTrade(trade, trade.UnrealisedPL > 0 ? 1 : -1, indicator.Candle.Time, indicator.Candle.Bid_L);
            }

            if (indicator.Candle.Bid_L <= trade.StopLoss && indicator.Candle.Bid_H >= trade.TakeProfit)
            {
                CloseTrade(trade, 0, indicator.Candle.Time, indicator.Candle.Mid_C);
            }

            trade.UnrealisedPL = trailingStop ? indicator.Candle.Bid_C - trade.TriggerPrice : 0;
        }

        if (trade.Signal == Signal.Sell)
        {
            if (indicator.Candle.Ask_L <= trade.TakeProfit && indicator.Candle.Ask_H < trade.StopLoss)
            {
                CloseTrade(trade, 1, indicator.Candle.Time, indicator.Candle.Ask_L);
            }

            if (indicator.Candle.Ask_H >= trade.StopLoss && indicator.Candle.Ask_L > trade.TakeProfit)
            {
                CloseTrade(trade, trade.UnrealisedPL > 0 ? 1 : -1, indicator.Candle.Time, indicator.Candle.Ask_H);
            }

            if (indicator.Candle.Ask_H >= trade.StopLoss && indicator.Candle.Ask_L <= trade.TakeProfit)
            {
                CloseTrade(trade, 0, indicator.Candle.Time, indicator.Candle.Mid_C);
            }

            trade.UnrealisedPL = trailingStop ? indicator.Candle.Ask_C - trade.TriggerPrice : 0;
        }
    }

    private static void UpdateStopLoss(TradeResult trade, IndicatorBase indicator)
    {
        trade.StopLoss = indicator.Signal == Signal.Buy
            ? indicator.Candle.Ask_C
            : indicator.Candle.Bid_C;
    }

    private static void CloseTrade(TradeResult trade, double result, DateTime endTime, double triggerPrice)
    {
        trade.Running = false;
        trade.Result = result;
        trade.EndTime = endTime;
        trade.TriggerPrice = triggerPrice;
    }
}