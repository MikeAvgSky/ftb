﻿namespace Trading.Bot.Extensions;

public static class CandleIndicators
{
    private const double RsiLimit = 50.0;
    private const double ProfitFactor = 1.5;

    public static MacResult[] CalcMac(this Candle[] candles, int shortWindow = 10, int longWindow = 20)
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

            result[i].Signal = result[i].Delta switch
            {
                >= 0 when result[i].DeltaPrev < 0 => Signal.Buy,
                < 0 when result[i].DeltaPrev >= 0 => Signal.Sell,
                _ => Signal.None
            };

            var diff = i < length - 1
                ? candles[i + 1].Mid_C - candles[i].Mid_C
                : candles[i].Mid_C;

            result[i].Gain = Math.Abs(diff * (int)result[i].Signal);

            result[i].TakeProfit = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C + result[i].Gain,
                Signal.Sell => candles[i].Mid_C - result[i].Gain,
                _ => 0.0
            };

            result[i].StopLoss = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C - result[i].Gain / ProfitFactor,
                Signal.Sell => candles[i].Mid_C + result[i].Gain / ProfitFactor,
                _ => 0.0
            };

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }

    public static BollingerBandsResult[] CalcBollingerBands(this Candle[] candles, int window = 20, int stdDev = 2)
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

            result[i].Signal = candles[i] switch
            {
                var candle when candle.Mid_C < result[i].LowerBand &&
                                candle.Mid_O > result[i].LowerBand => Signal.Buy,
                var candle when candle.Mid_C > result[i].UpperBand &&
                                candle.Mid_O < result[i].UpperBand => Signal.Sell,
                _ => Signal.None
            };

            result[i].Gain = Math.Abs(candles[i].Mid_C - result[i].Sma);

            result[i].TakeProfit = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C + result[i].Gain,
                Signal.Sell => candles[i].Mid_C - result[i].Gain,
                _ => 0.0
            };

            result[i].StopLoss = result[i].Signal switch
            {
                Signal.Buy => candles[i].Mid_C - result[i].Gain / ProfitFactor,
                Signal.Sell => candles[i].Mid_C + result[i].Gain / ProfitFactor,
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

        var maxTra = result.Select(x => x.MaxTr).CalcSma(window).ToArray();

        for (var i = 0; i < length; i++)
        {
            result[i].Atr = maxTra[i];
        }

        return result;
    }

    public static KeltnerResult[] CalcKeltner(this Candle[] candles, int emaWindow = 20, int atrWindow = 10)
    {
        var ema = candles.Select(c => c.Mid_C).CalcEma(emaWindow).ToArray();

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
        var emaShort = candles.Select(c => c.Mid_C).CalcEma(shortWindow).ToArray();

        var emaLong = candles.Select(c => c.Mid_C).CalcEma(longWindow).ToArray();

        var length = candles.Length;

        var result = new MacdResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new MacdResult();

            result[i].Candle = candles[i];

            result[i].Macd = emaShort[i] - emaLong[i];
        }

        var ema = result.Select(m => m.Macd).CalcEma(signal).ToArray();

        for (var i = 0; i < length; i++)
        {
            result[i].SignalLine = ema[i];

            result[i].Histogram = result[i].Macd - result[i].SignalLine;
        }

        return result;
    }

    public static RsiEmaResult[] CalcRsiEma(this Candle[] candles, int rsiWindow = 14, int emaWindow = 200)
    {
        var rsi = candles.CalcRsi(rsiWindow);

        var ema = candles.Select(c => c.Mid_C).CalcEma(emaWindow).ToArray();

        var length = candles.Length;

        var result = new RsiEmaResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new RsiEmaResult();

            result[i].Candle = candles[i];

            result[i].Rsi = rsi[i].Rsi;

            result[i].Ema = ema[i];

            var engulfing = i > 0 && candles[i].IsEngulfingCandle(candles[i - 1]);

            result[i].Signal = engulfing switch
            {
                true when candles[i].Direction == 1 && candles[i].Mid_L > result[i].Ema && result[i].Rsi > RsiLimit => Signal.Buy,
                true when candles[i].Direction == -1 && candles[i].Mid_H < result[i].Ema && result[i].Rsi < RsiLimit => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = result[i].Signal switch
            {
                Signal.Buy => (candles[i].Ask_C - candles[i].Ask_O) * ProfitFactor + candles[i].Ask_C,
                Signal.Sell => (candles[i].Bid_C - candles[i].Bid_O) * ProfitFactor + candles[i].Bid_C,
                _ => 0.0
            };

            result[i].Gain = Math.Abs(result[i].TakeProfit - candles[i].Ask_C);

            result[i].StopLoss = result[i].Signal switch
            {
                Signal.Buy => candles[i].Ask_O,
                Signal.Sell => candles[i].Bid_O,
                _ => 0.0
            };

            result[i].Loss = Math.Abs(candles[i].Bid_C - result[i].StopLoss);
        }

        return result;
    }

    public static MacdEmaResult[] CalcMacdEma(this Candle[] candles, int emaWindow = 100)
    {
        var macd = candles.CalcMacd();

        var ema = candles.Select(c => c.Mid_C).CalcEma(emaWindow).ToArray();

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

            result[i].Signal = result[i].Direction switch
            {
                1 when candles[i].Mid_L > result[i].Ema => Signal.Buy,
                -1 when candles[i].Mid_H < result[i].Ema => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = result[i].Signal switch
            {
                Signal.Buy => (candles[i].Ask_C - candles[i].Ask_O) * ProfitFactor + candles[i].Ask_C,
                Signal.Sell => (candles[i].Bid_C - candles[i].Bid_O) * ProfitFactor + candles[i].Bid_C,
                _ => 0.0
            };

            result[i].Gain = Math.Abs(result[i].TakeProfit - candles[i].Ask_C);

            result[i].StopLoss = result[i].Signal switch
            {
                Signal.Buy => candles[i].Ask_O,
                Signal.Sell => candles[i].Bid_O,
                _ => 0.0
            };

            result[i].Loss = Math.Abs(candles[i].Bid_C - result[i].StopLoss);
        }

        return result;
    }
}