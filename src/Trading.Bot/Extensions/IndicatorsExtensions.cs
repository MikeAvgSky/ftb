namespace Trading.Bot.Extensions;

public static class IndicatorsExtensions
{
    private const double RsiLimit = 50.0;
    private const double LossFactor = -1.0;
    private const double ProfitFactor = 1.5;

    public static IEnumerable<double> CalcCma(this IEnumerable<double> sequence)
    {
        if (sequence == null)
        {
            yield break;
        }

        double total = 0;

        var count = 0;

        foreach (var d in sequence)
        {
            count++;

            total += d;

            yield return total / count;
        }
    }

    public static IEnumerable<double> CalcSma(this IEnumerable<double> sequence, int window)
    {
        if (sequence == null)
        {
            yield break;
        }

        var queue = new Queue<double>(window);

        foreach (var d in sequence)
        {
            if (queue.Count == window)
            {
                queue.Dequeue();
            }

            queue.Enqueue(d);

            yield return queue.Average();
        }
    }

    public static IEnumerable<double> CalcEma(this IEnumerable<double> sequence, int window)
    {
        if (sequence == null)
        {
            yield break;
        }

        var list = sequence.ToArray();

        if (!list.Any())
        {
            yield return 0;
        }

        var alpha = 2.0 / (window + 1);

        var result = 0.0;

        for (var i = 0; i < list.Length; i++)
        {
            result = i == 0
                ? list[i]
                : alpha * list[i] + (1 - alpha) * result;

            yield return result;
        }
    }

    public static IEnumerable<double> CalcRma(this IEnumerable<double> sequence, int window)
    {
        if (sequence == null)
        {
            yield break;
        }

        var list = sequence.ToArray();

        if (!list.Any())
        {
            yield return 0;
        }

        var alpha = 1.0 / window;

        var result = 0.0;

        for (var i = 0; i < list.Length; i++)
        {
            result = i == 0
                ? list[i]
                : alpha * list[i] + (1 - alpha) * result;

            yield return result;
        }
    }

    public static double CalcStdDev(this IEnumerable<double> sequence, int std)
    {
        if (sequence == null)
        {
            return 0;
        }

        var list = sequence.ToArray();

        if (!list.Any())
        {
            return 0;
        }

        var average = list.Average();

        var sum = list.Sum(d => Math.Pow(d - average, std));

        return Math.Sqrt(sum / list.Length);
    }

    public static IEnumerable<double> CalcRolStdDev(this IEnumerable<double> sequence, int window, int std)
    {
        var queue = new Queue<double>(window);

        foreach (var d in sequence)
        {
            if (queue.Count == window)
            {
                queue.Dequeue();
            }

            queue.Enqueue(d);

            yield return queue.CalcStdDev(std);
        }
    }

    public static MacResult[] CalcMac(this Candle[] candles, int shortWindow = 10, int longWindow = 20)
    {
        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var maShort = typicalPrice.CalcSma(shortWindow).ToArray();

        var maLong = typicalPrice.CalcSma(longWindow).ToArray();

        var length = candles.Length;

        var mac = new MacResult[length];

        for (var i = 0; i < length; i++)
        {
            mac[i].MaShort = maShort[i];

            mac[i].MaLong = maLong[i];

            mac[i].Delta = maShort[i] - maLong[i];

            mac[i].DeltaPrev = i > 0 ? mac[i - 1].Delta : 0;

            mac[i].Signal = mac[i].Delta switch
            {
                >= 0 when mac[i].DeltaPrev < 0 => Signal.Buy,
                < 0 when mac[i].DeltaPrev >= 0 => Signal.Sell,
                _ => Signal.None
            };

            var diff = i < length - 1
                ? candles[i + 1].Mid_C - candles[i].Mid_C
                : candles[i].Mid_C;

            mac[i].Gain = Math.Abs(diff * (int)mac[i].Signal);

            mac[i].TakeProfit = mac[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C + mac[i].Gain,
                Signal.Sell => candles[i].Mid_C - mac[i].Gain,
                _ => 0.0
            };

            mac[i].StopLoss = mac[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C - mac[i].Gain / ProfitFactor,
                Signal.Sell => candles[i].Mid_C + mac[i].Gain / ProfitFactor,
                _ => 0.0
            };

            mac[i].Loss = Math.Abs(candles[i].Mid_C - mac[i].StopLoss);
        }

        return mac;
    }

    public static BollingerBandsResult[] CalcBollingerBands(this Candle[] candles, int window = 20, int stdDev = 2)
    {
        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var standardDeviation = typicalPrice.CalcRolStdDev(window, stdDev).ToArray();

        var bollingerBandsSma = typicalPrice.CalcSma(window).ToArray();

        var length = candles.Length;

        var bollingerBands = new BollingerBandsResult[length];

        for (var i = 0; i < length; i++)
        {
            bollingerBands[i].BollingerSma = bollingerBandsSma[i];

            bollingerBands[i].BollingerTop = bollingerBands[i].BollingerSma + standardDeviation[i] * stdDev;

            bollingerBands[i].BollingerBottom = bollingerBands[i].BollingerSma - standardDeviation[i] * stdDev;

            bollingerBands[i].Signal = candles[i] switch
            {
                var candle when candle.Mid_C < bollingerBands[i].BollingerBottom &&
                                candle.Mid_O > bollingerBands[i].BollingerBottom => Signal.Buy,
                var candle when candle.Mid_C > bollingerBands[i].BollingerTop &&
                                candle.Mid_O < bollingerBands[i].BollingerTop => Signal.Sell,
                _ => Signal.None
            };

            bollingerBands[i].Gain = Math.Abs(candles[i].Mid_C - bollingerBands[i].BollingerSma);

            bollingerBands[i].TakeProfit = bollingerBands[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C + bollingerBands[i].Gain,
                Signal.Sell => candles[i].Mid_C - bollingerBands[i].Gain,
                _ => 0.0
            };

            bollingerBands[i].StopLoss = bollingerBands[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C - bollingerBands[i].Gain / ProfitFactor,
                Signal.Sell => candles[i].Mid_C + bollingerBands[i].Gain / ProfitFactor,
                _ => 0.0
            };

            bollingerBands[i].Loss = Math.Abs(candles[i].Mid_C - bollingerBands[i].StopLoss);
        }

        return bollingerBands;
    }

    public static AtrResult[] CalcAtr(this Candle[] candles, int window = 14)
    {
        var length = candles.Length;

        var atr = new AtrResult[length];

        for (var i = 0; i < length; i++)
        {
            var prevMidC = i == 0 ? candles[i].Mid_C : candles[i - 1].Mid_C;

            var tr1 = candles[i].Mid_H - candles[i].Mid_L;

            var tr2 = Math.Abs(candles[i].Mid_H - prevMidC);

            var tr3 = Math.Abs(prevMidC - candles[i].Mid_L);

            var trueRanges = new[] { tr1, tr2, tr3 };

            atr[i].MaxTr = trueRanges.Max();
        }

        var maxTra = atr.Select(x => x.MaxTr).CalcSma(window).ToArray();

        for (var i = 0; i < length; i++)
        {
            atr[i].Atr = maxTra[i];
        }

        return atr;
    }

    public static KeltnerResult[] CalcKeltner(this Candle[] candles, int emaWindow = 20, int atrWindow = 10)
    {
        var ema = candles.Select(c => c.Mid_C).CalcEma(emaWindow).ToArray();

        var atr = candles.CalcAtr(atrWindow).ToArray();

        var length = candles.Length;

        var keltner = new KeltnerResult[length];

        for (var i = 0; i < length; i++)
        {
            keltner[i].Ema = ema[i];

            keltner[i].KeltnerTop = atr[i].Atr * 2 + ema[i];

            keltner[i].KeltnerBottom = keltner[i].Ema - atr[i].Atr * 2;
        }

        return keltner;
    }

    public static RsiResult[] CalcRsi(this Candle[] candles, int window = 14)
    {
        var length = candles.Length;

        var gains = new double[length];

        var losses = new double[length];

        var lastValue = 0.0;

        for (var i = 0; i < length; i++)
        {
            if (i == 0)
            {
                gains[i] = 0.0;

                losses[i] = 0.0;

                lastValue = candles[i].Mid_C;

                continue;
            }

            gains[i] = candles[i].Mid_C > lastValue ? candles[i].Mid_C - lastValue : 0.0;

            losses[i] = candles[i].Mid_C < lastValue ? lastValue - candles[i].Mid_C : 0.0;

            lastValue = candles[i].Mid_C;
        }

        var gains_rma = gains.CalcRma(window).ToArray();

        var losses_rma = losses.CalcRma(window).ToArray();

        var rsi = new RsiResult[length];

        for (var i = 0; i < length; i++)
        {
            rsi[i].AverageGain = gains_rma[i];

            rsi[i].AverageLoss = losses_rma[i];

            var rs = rsi[i].AverageGain / rsi[i].AverageLoss;

            rsi[i].Rsi = 100.0 - 100.0 / (1.0 + rs);
        }

        return rsi;
    }

    public static MacdResult[] CalcMacd(this Candle[] candles, int shortWindow = 12, int longWindow = 26, int signal = 9)
    {
        var emaShort = candles.Select(c => c.Mid_C).CalcEma(shortWindow).ToArray();

        var emaLong = candles.Select(c => c.Mid_C).CalcEma(longWindow).ToArray();

        var length = candles.Length;

        var macd = new MacdResult[length];

        for (var i = 0; i < length; i++)
        {
            macd[i].Macd = emaShort[i] - emaLong[i];
        }

        var ema = macd.Select(m => m.Macd).CalcEma(signal).ToArray();

        for (var i = 0; i < length; i++)
        {
            macd[i].MacdSignal = ema[i];

            macd[i].Histogram = macd[i].Macd - macd[i].MacdSignal;
        }

        return macd;
    }

    public static RsiEmaResult[] CalcRsiEma(this Candle[] candles, int rsiWindow = 14, int emaWindow = 200)
    {
        var rsi = candles.CalcRsi(rsiWindow);

        var ema = candles.Select(c => c.Mid_C).CalcEma(emaWindow).ToArray();

        var length = candles.Length;

        var rsiEma = new RsiEmaResult[length];

        for (var i = 0; i < length; i++)
        {
            rsiEma[i].Rsi = rsi[i].Rsi;

            rsiEma[i].Ema = ema[i];

            var engulfing = i > 0 && candles[i].IsEngulfingCandle(candles[i - 1]);

            rsiEma[i].Signal = engulfing switch
            {
                true when candles[i].Direction == 1 && candles[i].Mid_L > rsiEma[i].Ema && rsiEma[i].Rsi > RsiLimit => Signal.Buy,
                true when candles[i].Direction == -1 && candles[i].Mid_H < rsiEma[i].Ema && rsiEma[i].Rsi < RsiLimit => Signal.Sell,
                _ => Signal.None
            };

            rsiEma[i].TakeProfit = rsiEma[i].Signal switch
            {
                Signal.Buy or Signal.Sell => (candles[i].Mid_C - candles[i].Mid_O) * ProfitFactor + candles[i].Mid_C,
                _ => 0.0
            };

            rsiEma[i].StopLoss = rsiEma[i].Signal switch
            {
                Signal.Buy or Signal.Sell => candles[i].Mid_O,
                _ => 0.0
            };
        }

        return rsiEma;
    }

    public static TradeResult[] CalcTradeResults(this Candle[] candles, Indicator[] indicators)
    {
        var length = candles.Length;

        var tr = new TradeResult[length];

        var openTrades = new List<TradeResult>();

        var closeTrades = new List<TradeResult>();

        for (var i = 0; i < length; i++)
        {
            tr[i].Running = true;

            tr[i].StartIndex = i;

            tr[i].StartPrice = candles[i].Mid_C;

            tr[i].Signal = indicators[i].Signal;

            tr[i].TakeProfit = indicators[i].TakeProfit;

            tr[i].StopLoss = indicators[i].StopLoss;

            tr[i].StartTime = candles[i].Time;

            if (tr[i].Signal != Signal.None)
            {
                openTrades.Add(tr[i]);
            }
        }

        foreach (var trade in openTrades)
        {
            var index = Array.IndexOf(tr, trade);

            if (trade.Signal == Signal.Buy)
            {
                if (candles[index].Bid_H >= trade.TakeProfit)
                {
                    CloseTrade(trade, ProfitFactor, candles[index].Time, candles[index].Bid_H);
                }
                else if (candles[index].Bid_L <= trade.StopLoss)
                {
                    CloseTrade(trade, LossFactor, candles[index].Time, candles[index].Bid_L);
                }
            }

            if (trade.Signal == Signal.Sell)
            {
                if (candles[index].Ask_L <= trade.TakeProfit)
                {
                    CloseTrade(trade, ProfitFactor, candles[index].Time, candles[index].Ask_L);
                }
                else if (candles[index].Ask_H >= trade.StopLoss)
                {
                    CloseTrade(trade, LossFactor, candles[index].Time, candles[index].Ask_H);
                    trade.Running = false;
                    trade.Result = LossFactor;
                    trade.EndTime = candles[index].Time;
                    trade.TriggerPrice = candles[index].Ask_H;
                }
            }

            if (!trade.Running)
            {
                closeTrades.Add(trade);
            }
        }

        return closeTrades.ToArray();
    }

    private static void CloseTrade(TradeResult trade, double result, DateTime endTime, double triggerPrice)
    {
        trade.Running = false;
        trade.Result = result;
        trade.EndTime = endTime;
        trade.TriggerPrice = triggerPrice;
    }
}