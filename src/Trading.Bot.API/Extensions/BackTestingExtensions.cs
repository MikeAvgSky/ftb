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

    public static IEnumerable<FileData<IEnumerable<object>>> GetFileData(this IndicatorResult[] indicator, string fileName, int tradeRisk, double riskReward, bool updateTrade = false)
    {
        var fileData = new List<FileData<IEnumerable<object>>>();

        var tradingSim = SimulateTrade(indicator.Cast<IndicatorBase>().ToArray(), tradeRisk, riskReward, updateTrade);

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

    private static (TradeResult[] Result, SimulationSummary Summary) SimulateTrade(IndicatorBase[] indicators, int tradeRisk, double riskReward, bool updateTrade)
    {
        var length = indicators.Length;

        var openTrades = new List<TradeResult>();

        var closedTrades = new List<TradeResult>();

        for (var i = 0; i < length; i++)
        {
            UpdateUnrealisedPl(indicators[i], openTrades);

            if (updateTrade && indicators[i].Signal != Signal.None && openTrades.Count > 0)
            {
                foreach (var trade in openTrades.Where(trade => DifferentDirection(indicators[i].Signal, trade.Signal)))
                {
                    UpdateTrade(trade, indicators[i]);

                    var triggerPrice = trade.Signal == Signal.Buy
                        ? indicators[i].Candle.Bid_C
                        : indicators[i].Candle.Ask_C;

                    if (!trade.Running || !(trade.UnrealisedPL > 0)) continue;

                    CloseTrade(trade, 1, indicators[i].Candle.Time, triggerPrice);

                    closedTrades.Add(trade);
                }

                openTrades.RemoveAll(ot => !ot.Running);

                if (openTrades.Count > 0) continue;
            }

            if (indicators[i].Signal != Signal.None && openTrades.Count == 0)
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

            UpdateTrades(indicators[i], updateTrade, openTrades, closedTrades);

            openTrades.RemoveAll(ot => !ot.Running);
        }

        var summary = CalcSimSummary(indicators, tradeRisk, riskReward, closedTrades);

        return (closedTrades.ToArray(), summary);
    }

    private static bool DifferentDirection(Signal signal, Signal tradeSignal) => signal != tradeSignal;

    private static void UpdateUnrealisedPl(IndicatorBase indicator, List<TradeResult> openTrades)
    {
        foreach (var trade in openTrades)
        {
            trade.UnrealisedPL = trade.Signal switch
            {
                Signal.Buy => indicator.Candle.Bid_C - trade.TriggerPrice,
                Signal.Sell => indicator.Candle.Ask_C - trade.TriggerPrice,
                _ => trade.UnrealisedPL
            };
        }
    }

    private static void UpdateTrades(IndicatorBase indicator, bool updateTrade, List<TradeResult> openTrades, List<TradeResult> closedTrades)
    {
        foreach (var trade in openTrades)
        {
            UpdateTrade(trade, indicator);

            if (ShouldUpdateStopLoss(updateTrade, trade, indicator))
                trade.StopLoss = trade.TriggerPrice;

            if (trade.Running) continue;

            closedTrades.Add(trade);
        }
    }

    private static void UpdateTrade(TradeResult trade, IndicatorBase indicator)
    {
        if (trade.Signal == Signal.Buy)
        {
            if (indicator.Candle.Bid_H >= trade.TakeProfit && indicator.Candle.Bid_L > trade.StopLoss)
            {
                CloseTrade(trade, 1, indicator.Candle.Time, indicator.Candle.Bid_H);
            }

            if (indicator.Candle.Bid_L <= trade.StopLoss && indicator.Candle.Bid_H < trade.TakeProfit)
            {
                CloseTrade(trade, GetLossResult(trade), indicator.Candle.Time, indicator.Candle.Bid_L);
            }

            if (indicator.Candle.Bid_L <= trade.StopLoss && indicator.Candle.Bid_H >= trade.TakeProfit)
            {
                CloseTrade(trade, double.NaN, indicator.Candle.Time, indicator.Candle.Mid_C);
            }
        }

        if (trade.Signal == Signal.Sell)
        {
            if (indicator.Candle.Ask_L <= trade.TakeProfit && indicator.Candle.Ask_H < trade.StopLoss)
            {
                CloseTrade(trade, 1, indicator.Candle.Time, indicator.Candle.Ask_L);
            }

            if (indicator.Candle.Ask_H >= trade.StopLoss && indicator.Candle.Ask_L > trade.TakeProfit)
            {
                CloseTrade(trade, GetLossResult(trade), indicator.Candle.Time, indicator.Candle.Ask_H);
            }

            if (indicator.Candle.Ask_H >= trade.StopLoss && indicator.Candle.Ask_L <= trade.TakeProfit)
            {
                CloseTrade(trade, double.NaN, indicator.Candle.Time, indicator.Candle.Mid_C);
            }
        }
    }

    private static bool ShouldUpdateStopLoss(bool updateTrade, TradeResult trade, IndicatorBase indicator)
    {
        var priceList = new List<double> { trade.TriggerPrice, trade.TakeProfit };

        var currentValue = trade.Signal == Signal.Buy
            ? indicator.Candle.Ask_C
            : indicator.Candle.Bid_C;

        var closest = priceList.OrderBy(value => Math.Abs(currentValue - value)).First();

        return updateTrade && trade.Running && trade.StopLoss < trade.TriggerPrice && trade.TakeProfit - closest == 0;
    }

    private static int GetLossResult(TradeResult trade)
    {
        if (trade.StopLoss - trade.TriggerPrice == 0) return 0;

        return -1;
    }

    private static void CloseTrade(TradeResult trade, double result, DateTime endTime, double triggerPrice)
    {
        trade.Running = false;
        trade.Result = Math.Round(GetAccurateResult(result, trade, triggerPrice), 2);
        trade.EndTime = endTime;
        trade.TriggerPrice = triggerPrice;
    }

    private static double GetAccurateResult(double result, TradeResult trade, double triggerPrice)
    {
        if (double.IsNaN(result) || result - 1 != 0) return result;

        return trade.Signal switch
        {
            Signal.Buy => triggerPrice >= trade.TakeProfit ? 1 : GetDistance(triggerPrice, trade.TakeProfit),
            Signal.Sell => triggerPrice <= trade.TakeProfit ? 1 : GetDistance(triggerPrice, trade.TakeProfit),
            _ => result
        };
    }

    private static double GetDistance(double triggerPrice, double takeProfit)
    {
        return Math.Abs(triggerPrice - takeProfit) / ((triggerPrice + takeProfit) / 2) * 100;
    }

    private static SimulationSummary CalcSimSummary(IndicatorBase[] indicators, int tradeRisk, double riskReward, List<TradeResult> closedTrades)
    {
        var summary = new SimulationSummary
        {
            Days = indicators.Last().Candle.Time.Subtract(indicators.First().Candle.Time).Days,
            Candles = indicators.Length,
            Trades = closedTrades.Count,
            Wins = closedTrades.Count(t => t.Result > 0),
            Losses = closedTrades.Count(t => t.Result < 0),
            Unknown = closedTrades.Count(t => double.IsNaN(t.Result)),
            Even = closedTrades.Count(t => t.Result == 0),
            TradeRisk = tradeRisk
        };

        summary.WinRate = Math.Round((double)summary.Wins * 100 / (summary.Trades - summary.Unknown - summary.Even), 2);

        var buyWins = closedTrades.Count(t => t.Result > 0 && t.Signal == Signal.Buy);

        var buyTrades = closedTrades.Count(t => !double.IsNaN(t.Result) && t.Result != 0 && t.Signal == Signal.Buy);

        summary.BuyWinRate = Math.Round((double)buyWins * 100 / buyTrades, 2);

        var sellWins = closedTrades.Count(t => t.Result > 0 && t.Signal == Signal.Sell);

        var sellTrades = closedTrades.Count(t => !double.IsNaN(t.Result) && t.Result != 0 && t.Signal == Signal.Sell);

        summary.SellWinRate = Math.Round((double)sellWins * 100 / sellTrades, 2);

        var winResultSum = Math.Round(closedTrades.Where(t => t.Result > 0).Sum(t => t.Result));

        summary.Balance = Math.Round(winResultSum * (tradeRisk * riskReward) - summary.Losses * tradeRisk, 2);

        return summary;
    }
}