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

            result[i].Signal = i < length - 1
                ? Signal.None
                : result[i].Delta switch
                {
                    >= 0 when result[i].DeltaPrev < 0 &&
                    candles[i].Spread <= maxSpread &&
                    result[i].Gain >= minGain => Signal.Buy,
                    < 0 when result[i].DeltaPrev >= 0 &&
                    candles[i].Spread <= maxSpread &&
                    result[i].Gain >= minGain => Signal.Sell,
                    _ => Signal.None
                };

            result[i].TakeProfit = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C + result[i].Gain,
                Signal.Sell => candles[i].Mid_C - result[i].Gain,
                _ => 0.0
            };

            result[i].StopLoss = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C - result[i].Gain / riskReward,
                Signal.Sell => candles[i].Mid_C + result[i].Gain / riskReward,
                _ => 0.0
            };

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

            result[i].Signal = i < length - 1
                ? Signal.None
                : candles[i] switch
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

            result[i].TakeProfit = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C + result[i].Gain,
                Signal.Sell => candles[i].Mid_C - result[i].Gain,
                _ => 0.0
            };

            result[i].StopLoss = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C - result[i].Gain / riskReward,
                Signal.Sell => candles[i].Mid_C + result[i].Gain / riskReward,
                _ => 0.0
            };

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

            result[i].Signal = i < length - 1
                ? Signal.None
                : candles[i] switch
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

            result[i].TakeProfit = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C + result[i].Gain,
                Signal.Sell => candles[i].Mid_C - result[i].Gain,
                _ => 0.0
            };

            result[i].StopLoss = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C - result[i].Gain / riskReward,
                Signal.Sell => candles[i].Mid_C + result[i].Gain / riskReward,
                _ => 0.0
            };

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

            var rs = result[i].AverageGain / result[i].AverageLoss;

            result[i].Rsi = 100.0 - 100.0 / (1.0 + rs);
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

    public static RsiEmaResult[] CalcRsiEma(this Candle[] candles, int rsiWindow = 14, int emaWindow = 200, double rsiLimit = 50.0,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var rsi = candles.CalcRsi(rsiWindow);

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var ema = prices.CalcEma(emaWindow).ToArray();

        var length = candles.Length;

        var result = new RsiEmaResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new RsiEmaResult();

            result[i].Candle = candles[i];

            result[i].Rsi = rsi[i].Rsi;

            result[i].Ema = ema[i];

            var engulfing = i > 0 && candles[i].IsEngulfingCandle(candles[i - 1]);

            result[i].Gain = Math.Abs(candles[i].Mid_C - result[i].Ema);

            result[i].Signal = i < length - 1
                ? Signal.None
                : engulfing switch
                {
                    true when candles[i].Direction == 1 &&
                              candles[i].Mid_L > result[i].Ema &&
                              result[i].Rsi > rsiLimit &&
                              candles[i].Spread <= maxSpread &&
                              result[i].Gain >= minGain => Signal.Buy,
                    true when candles[i].Direction == -1 &&
                              candles[i].Mid_H < result[i].Ema &&
                              result[i].Rsi < rsiLimit &&
                              candles[i].Spread <= maxSpread &&
                              result[i].Gain >= minGain => Signal.Sell,
                    _ => Signal.None
                };

            result[i].TakeProfit = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C + result[i].Gain,
                Signal.Sell => candles[i].Mid_C - result[i].Gain,
                _ => 0.0
            };

            result[i].StopLoss = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C - result[i].Gain / riskReward,
                Signal.Sell => candles[i].Mid_C + result[i].Gain / riskReward,
                _ => 0.0
            };

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }

    public static MacdEmaResult[] CalcMacdEma(this Candle[] candles, int emaWindow = 100,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var macd = candles.CalcMacd();

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var ema = prices.CalcEma(emaWindow).ToArray();

        var length = candles.Length;

        var result = new MacdEmaResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new MacdEmaResult();

            result[i].Candle = candles[i];

            result[i].MacdDelta = macd[i].Macd - macd[i].SignalLine;

            result[i].MacdDeltaPrev = i == 0 ? 0.0 : macd[i - 1].Macd - macd[i - 1].SignalLine;

            result[i].Direction = result[i].MacdDelta switch
            {
                > 0 when result[i].MacdDeltaPrev < 0 => 1,
                < 0 when result[i].MacdDeltaPrev > 0 => -1,
                _ => 0
            };

            result[i].Ema = ema[i];

            result[i].Gain = Math.Abs(candles[i].Mid_C - result[i].Ema);

            result[i].Signal = i < length - 1
                ? Signal.None
                : result[i].Direction switch
                {
                    1 when candles[i].Mid_L > result[i].Ema &&
                           candles[i].Spread <= maxSpread &&
                           result[i].Gain >= minGain => Signal.Sell,
                    -1 when candles[i].Mid_H < result[i].Ema &&
                            candles[i].Spread <= maxSpread &&
                            result[i].Gain >= minGain => Signal.Buy,
                    _ => Signal.None
                };

            result[i].TakeProfit = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C + result[i].Gain,
                Signal.Sell => candles[i].Mid_C - result[i].Gain,
                _ => 0.0
            };

            result[i].StopLoss = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C - result[i].Gain / riskReward,
                Signal.Sell => candles[i].Mid_C + result[i].Gain / riskReward,
                _ => 0.0
            };

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}