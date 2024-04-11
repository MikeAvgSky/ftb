namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcRsiBbReversal(this Candle[] candles, int bbWindow = 30, int rsiWindow = 13, double stdDev = 2,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5, double rsiLower = 30, double rsiUpper = 70)
    {
        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var rolStdDev = typicalPrice.CalcRolStdDev(bbWindow, stdDev).ToArray();

        var sma = typicalPrice.CalcSma(bbWindow).ToArray();

        var rsiResult = candles.CalcRsi(rsiWindow);

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
                var candle when candle.Mid_C > upperBand &&
                                candle.Mid_O < upperBand &&
                                rsiResult[i].Rsi < rsiUpper &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Sell,
                var candle when candle.Mid_C < lowerBand &&
                                candle.Mid_O > lowerBand &&
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