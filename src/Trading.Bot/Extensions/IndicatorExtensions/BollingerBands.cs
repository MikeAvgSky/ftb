namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
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

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i]);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}