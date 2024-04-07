namespace Trading.Bot.Extensions;

public static class CandleIndicators
{
    public static MacResult[] CalcMac(this Candle[] candles, int shortWindow = 10, int longWindow = 20,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var maShort = typicalPrice.CalcSma(shortWindow).ToArray();

        var maLong = typicalPrice.CalcSma(longWindow).ToArray();

        var length = candles.Length;

        var result = new MacResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new MacResult();

            result[i].Candle = candles[i];

            result[i].MaShort = maShort[i];

            result[i].MaLong = maLong[i];

            result[i].Delta = maShort[i] - maLong[i];

            result[i].DeltaPrev = i > 0 ? result[i - 1].Delta : 0;

            result[i].Gain = Math.Abs(candles[i].Mid_C - result[i].MaShort);

            result[i].Signal = result[i].Delta switch
            {
                >= 0 when result[i].DeltaPrev < 0 &&
                candles[i].Spread <= maxSpread &&
                result[i].Gain >= minGain => Signal.Buy,
                < 0 when result[i].DeltaPrev >= 0 &&
                candles[i].Spread <= maxSpread &&
                result[i].Gain >= minGain => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = CalcTakeProfit(candles[i], result[i]);

            result[i].StopLoss = CalcStopLoss(candles[i], result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }

    public static BollingerBandsResult[] CalcBollingerBands(this Candle[] candles, int window = 20, double stdDev = 2,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var rolStdDev = typicalPrice.CalcRolStdDev(window, stdDev).ToArray();

        var sma = typicalPrice.CalcSma(window).ToArray();

        var length = candles.Length;

        var result = new BollingerBandsResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new BollingerBandsResult();

            result[i].Candle = candles[i];

            result[i].Sma = sma[i];

            result[i].UpperBand = sma[i] + rolStdDev[i] * stdDev;

            result[i].LowerBand = sma[i] - rolStdDev[i] * stdDev;

            result[i].Gain = Math.Abs(candles[i].Mid_C - result[i].Sma);

            result[i].Signal = candles[i] switch
            {
                var candle when candle.Mid_C < result[i].LowerBand &&
                                candle.Mid_O > result[i].LowerBand &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Buy,
                var candle when candle.Mid_C > result[i].UpperBand &&
                                candle.Mid_O < result[i].UpperBand &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = CalcTakeProfit(candles[i], result[i]);

            result[i].StopLoss = CalcStopLoss(candles[i], result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }

    public static AtrResult[] CalcAtr(this Candle[] candles, int window = 14)
    {
        var length = candles.Length;

        var result = new AtrResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new AtrResult();

            var prevMidC = i == 0 ? candles[i].Mid_C : candles[i - 1].Mid_C;

            var tr1 = candles[i].Mid_H - candles[i].Mid_L;

            var tr2 = Math.Abs(candles[i].Mid_H - prevMidC);

            var tr3 = Math.Abs(prevMidC - candles[i].Mid_L);

            var trueRanges = new[] { tr1, tr2, tr3 };

            result[i].Candle = candles[i];

            result[i].MaxTr = trueRanges.Max();
        }

        var maxTra = result.Select(x => x.MaxTr).ToArray().CalcSma(window).ToArray();

        for (var i = 0; i < length; i++)
        {
            result[i].Atr = maxTra[i];
        }

        return result;
    }

    public static KeltnerResult[] CalcKeltner(this Candle[] candles, int emaWindow = 20, int atrWindow = 10,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var ema = prices.CalcEma(emaWindow).ToArray();

        var atr = candles.CalcAtr(atrWindow).ToArray();

        var length = candles.Length;

        var result = new KeltnerResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new KeltnerResult();

            result[i].Candle = candles[i];

            result[i].Ema = ema[i];

            result[i].UpperBand = atr[i].Atr * 2 + ema[i];

            result[i].LowerBand = result[i].Ema - atr[i].Atr * 2;

            result[i].Gain = Math.Abs(candles[i].Mid_C - result[i].Ema);

            result[i].Signal = candles[i] switch
            {
                var candle when candle.Mid_C < result[i].LowerBand &&
                                candle.Mid_O > result[i].LowerBand &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Buy,
                var candle when candle.Mid_C > result[i].UpperBand &&
                                candle.Mid_O < result[i].UpperBand &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = CalcTakeProfit(candles[i], result[i]);

            result[i].StopLoss = CalcStopLoss(candles[i], result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
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

        var result = new RsiResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new RsiResult();

            result[i].Candle = candles[i];

            result[i].AverageGain = gains_rma[i];

            result[i].AverageLoss = losses_rma[i];

            if (i > 0)
            {
                var rs = result[i].AverageGain / result[i].AverageLoss;

                result[i].Rsi = 100.0 - 100.0 / (1.0 + rs);
            }
            else
            {
                result[i].Rsi = 0.0;
            }
        }

        return result;
    }

    public static MacdResult[] CalcMacd(this Candle[] candles, int shortWindow = 12, int longWindow = 26, int signal = 9)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaShort = prices.CalcEma(shortWindow).ToArray();

        var emaLong = prices.CalcEma(longWindow).ToArray();

        var length = candles.Length;

        var result = new MacdResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new MacdResult();

            result[i].Candle = candles[i];

            result[i].Macd = emaShort[i] - emaLong[i];
        }

        var ema = result.Select(m => m.Macd).ToArray().CalcEma(signal).ToArray();

        for (var i = 0; i < length; i++)
        {
            result[i].SignalLine = ema[i];

            result[i].Histogram = result[i].Macd - result[i].SignalLine;
        }

        return result;
    }

    public static StochasticResult[] CalcStochastic(this Candle[] candles, int oscWindow = 14, int maWindow = 3)
    {
        var length = candles.Length;

        var result = new StochasticResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new StochasticResult();

            if (i < oscWindow - 1) continue;

            result[i].Candle = candles[i];

            var lastCandles = new Candle[oscWindow];

            Array.Copy(candles[..(i + 1)], i - (oscWindow - 1), lastCandles, 0, oscWindow);

            var highestPrice = lastCandles.Select(c => c.Mid_C).Max();

            var lowestPrice = lastCandles.Select(c => c.Mid_C).Min();

            result[i].FastOscillator = highestPrice - lowestPrice != 0
                ? 100 * (result[i].Candle.Mid_C - lowestPrice) / (highestPrice - lowestPrice)
                : 0.0;
        }

        var oscillators = result.Select(r => r.FastOscillator).ToArray();

        var sma = oscillators.CalcSma(maWindow).ToArray();

        for (var i = 0; i < length; i++)
        {
            if (i < oscWindow - 1)
            {
                result[i].SlowOscillator = 0.0;

                continue;
            }

            result[i].SlowOscillator = sma[i];
        }

        return result;
    }

    public static IndicatorResult[] CalcRsiEma(this Candle[] candles, int rsiWindow = 14, int emaWindow = 200, double rsiLimit = 50.0,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var rsiResult = candles.CalcRsi(rsiWindow);

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaResult = prices.CalcEma(emaWindow).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var rsi = rsiResult[i].Rsi;

            var ema = emaResult[i];

            var engulfing = i > 0 && candles[i].IsEngulfingCandle(candles[i - 1]);

            result[i].Gain = Math.Abs(candles[i].Mid_C - ema);

            result[i].Signal = engulfing switch
            {
                true when candles[i].Direction == 1 &&
                          candles[i].Mid_L > ema &&
                          rsi > rsiLimit &&
                          candles[i].Spread <= maxSpread &&
                          result[i].Gain >= minGain => Signal.Buy,
                true when candles[i].Direction == -1 &&
                          candles[i].Mid_H < ema &&
                          rsi < rsiLimit &&
                          candles[i].Spread <= maxSpread &&
                          result[i].Gain >= minGain => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = CalcTakeProfit(candles[i], result[i]);

            result[i].StopLoss = CalcStopLoss(candles[i], result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }

    public static IndicatorResult[] CalcMacdEma(this Candle[] candles, int emaWindow = 100,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var macd = candles.CalcMacd();

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaResult = prices.CalcEma(emaWindow).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var macDelta = macd[i].Macd - macd[i].SignalLine;

            var macDeltaPrev = i == 0 ? 0.0 : macd[i - 1].Macd - macd[i - 1].SignalLine;

            var direction = macDelta switch
            {
                > 0 when macDeltaPrev < 0 => 1,
                < 0 when macDeltaPrev > 0 => -1,
                _ => 0
            };

            var ema = emaResult[i];

            result[i].Gain = Math.Abs(candles[i].Mid_C - ema);

            result[i].Signal = direction switch
            {
                1 when candles[i].Mid_L > ema &&
                       candles[i].Spread <= maxSpread &&
                       result[i].Gain >= minGain => Signal.Buy,
                -1 when candles[i].Mid_H < ema &&
                        candles[i].Spread <= maxSpread &&
                        result[i].Gain >= minGain => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = CalcTakeProfit(candles[i], result[i]);

            result[i].StopLoss = CalcStopLoss(candles[i], result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }

    public static IndicatorResult[] CalcStochRsiBands(this Candle[] candles, int bbWindow = 30, int rsiWindow = 13, double stdDev = 2,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5, double lower = 25, double upper = 75)
    {
        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var rolStdDev = typicalPrice.CalcRolStdDev(bbWindow, stdDev).ToArray();

        var sma = typicalPrice.CalcSma(bbWindow).ToArray();

        var rsiResult = candles.CalcRsi(rsiWindow);

        var stochastic = candles.CalcStochastic(rsiWindow);

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var upperBand = sma[i] + rolStdDev[i] * stdDev;

            var lowerBand = sma[i] - rolStdDev[i] * stdDev;

            result[i].Gain = Math.Abs(candles[i].Mid_C - sma[i]);

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when candle.Mid_C < lowerBand &&
                                candle.Mid_O > lowerBand &&
                                rsiResult[i].Rsi < lower &&
                                stochastic[i].FastOscillator < lower &&
                                stochastic[i].SlowOscillator < lower &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Sell,
                var candle when candle.Mid_C > upperBand &&
                                candle.Mid_O < upperBand &&
                                rsiResult[i].Rsi > upper &&
                                stochastic[i].FastOscillator > upper &&
                                stochastic[i].SlowOscillator > upper &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Buy,
                _ => Signal.None
            };

            result[i].TakeProfit = CalcTakeProfit(candles[i], result[i]);

            result[i].StopLoss = CalcStopLoss(candles[i], result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }

    public static IndicatorResult[] CalcMacdTripleMa(this Candle[] candles, int shortEma = 8, int longEma = 21,
        int longSma = 50, double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var macd = candles.CalcMacd();

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var shortEmaResult = prices.CalcEma(shortEma).ToArray();

        var longEmaResult = prices.CalcEma(longEma).ToArray();

        var longSmaResult = prices.CalcEma(longSma).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var macDelta = macd[i].Macd - macd[i].SignalLine;

            var macDeltaPrev = i == 0 ? 0.0 : macd[i - 1].Macd - macd[i - 1].SignalLine;

            var direction = macDelta switch
            {
                > 0 when macDeltaPrev < 0 => 1,
                < 0 when macDeltaPrev > 0 => -1,
                _ => 0
            };

            result[i].Gain = Math.Abs(candles[i].Mid_C - longEmaResult[i]);

            result[i].Signal = direction switch
            {
                1 when shortEmaResult[i] > longEmaResult[i] &&
                       longEmaResult[i] > longSmaResult[i] &&
                       candles[i].Spread <= maxSpread &&
                       result[i].Gain >= minGain => Signal.Buy,
                -1 when shortEmaResult[i] < longEmaResult[i] &&
                        longEmaResult[i] < longSmaResult[i] &&
                        candles[i].Spread <= maxSpread &&
                        result[i].Gain >= minGain => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = CalcTakeProfit(candles[i], result[i]);

            result[i].StopLoss = CalcStopLoss(candles[i], result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }

    private static double CalcTakeProfit(Candle candle, IndicatorBase result)
    {
        return result.Signal switch
        {
            Signal.Buy => candle.Mid_C + result.Gain,
            Signal.Sell => candle.Mid_C - result.Gain,
            _ => 0.0
        };
    }

    private static double CalcStopLoss(Candle candle, IndicatorBase result, double riskReward)
    {
        return result.Signal switch
        {
            Signal.Buy => candle.Mid_C - result.Gain / riskReward,
            Signal.Sell => candle.Mid_C + result.Gain / riskReward,
            _ => 0.0
        };
    }
}