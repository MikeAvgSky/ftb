namespace Trading.Bot.Extensions;

public static class IndicatorsExtensions
{
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

    public static IEnumerable<BollingerBandsResult> CalcBollingerBands(this Candle[] candles, int window, int std, double riskReward)
    {
        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var standardDeviation = typicalPrice.CalcRolStdDev(window, std).ToArray();

        var bollingerBandsSma = typicalPrice.CalcSma(window).ToArray();

        var length = candles.Length;

        var bollingerBands = new BollingerBandsResult[length];

        for (var i = 0; i < length; i++)
        {
            bollingerBands[i].BollingerSma = bollingerBandsSma[i];

            bollingerBands[i].BollingerTop = bollingerBands[i].BollingerSma + standardDeviation[i] * std;

            bollingerBands[i].BollingerBottom = bollingerBands[i].BollingerSma - standardDeviation[i] * std;

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
                Signal.Buy => candles[i].Mid_C - bollingerBands[i].Gain / riskReward,
                Signal.Sell => candles[i].Mid_C + bollingerBands[i].Gain / riskReward,
                _ => 0.0
            };

            bollingerBands[i].Loss = Math.Abs(candles[i].Mid_C - bollingerBands[i].StopLoss);
        }

        return bollingerBands;
    }

    public static IEnumerable<MacdResult> CalcMacd(this Candle[] candles, int shortWindow, int longWindow, int signal)
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
            macd[i].Signal = ema[i];

            macd[i].Histogram = macd[i].Macd - macd[i].Signal;
        }

        return macd;
    }

    public static IEnumerable<RsiResult> CalcRsi(this Candle[] candles, int window)
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

    public static IEnumerable<AtrResult> CalcAtr(this Candle[] candles, int window)
    {
        var length = candles.Length;

        var atr = new AtrResult[length];

        for (var i = 0; i < length; i++)
        {
            var prevMidC = i == 0 ? candles[i].Mid_C : candles[i - 1].Mid_C;

            atr[i].TrA = candles[i].Mid_H - candles[i].Mid_L;

            atr[i].TrB = Math.Abs(candles[i].Mid_H - prevMidC);

            atr[i].TrC = Math.Abs(prevMidC - candles[i].Mid_L);

            var trueRanges = new[] { atr[i].TrA, atr[i].TrB, atr[i].TrC };

            atr[i].MaxTr = trueRanges.Max();
        }

        var maxTra = atr.Select(x => x.MaxTr).CalcSma(window).ToArray();

        for (var i = 0; i < length; i++)
        {
            atr[i].Atr = maxTra[i];
        }

        return atr;
    }

    public static IEnumerable<KeltnerResult> CalcKeltner(this Candle[] candles, int window)
    {
        var ema = candles.Select(c => c.Mid_C).CalcEma(window).ToArray();

        var atr = candles.CalcAtr(window).ToArray();

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

    public static IEnumerable<MacResult> CalcMac(this Candle[] candles, int shortWindow, int longWindow, double riskReward)
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
                Signal.Buy => candles[i].Mid_C - mac[i].Gain / riskReward,
                Signal.Sell => candles[i].Mid_C + mac[i].Gain / riskReward,
                _ => 0.0
            };

            mac[i].Loss = Math.Abs(candles[i].Mid_C - mac[i].StopLoss);
        }

        return mac;
    }
}