﻿namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcRsiBbReversal(this Candle[] candles, int bbWindow = 30, int rsiWindow = 13, double stdDev = 2,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5, double rsiLower = 30, double rsiUpper = 70)
    {
        var rsiResult = candles.CalcRsi(rsiWindow);

        var bollingerBands = candles.CalcBollingerBands(bbWindow, stdDev);

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = Math.Abs(candles[i].Mid_C - bollingerBands[i].Sma);

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when candle.Mid_C > bollingerBands[i].UpperBand &&
                                candle.Mid_O < bollingerBands[i].UpperBand &&
                                rsiResult[i].Rsi < rsiUpper &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Sell,
                var candle when candle.Mid_C < bollingerBands[i].LowerBand &&
                                candle.Mid_O > bollingerBands[i].LowerBand &&
                                rsiResult[i].Rsi > rsiLower &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Buy,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i]);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}