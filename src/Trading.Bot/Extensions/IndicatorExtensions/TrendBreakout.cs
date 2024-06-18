namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcTrendBreakout(this Candle[] candles, int bbWindow = 20, int emaWindow = 100,
        double stdDev = 2, double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaResult = prices.CalcEma(emaWindow).ToArray();

        var bollingerBands = candles.CalcBollingerBands(bbWindow, stdDev);

        var length = candles.Length;

        var result = new IndicatorResult[length];

        var crossedLowerBand = false;

        var crossedUpperBand = false;

        var higherHighs = false;

        var higherLows = false;

        var lowerHighs = false;

        var lowerLows = false;

        var latestHigh = candles[0].Mid_C;

        var latestLow = candles[0].Mid_C;

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = minGain;

            if (candles[i].Mid_O > bollingerBands[i].LowerBand && candles[i].Mid_C < bollingerBands[i].LowerBand)
            {
                crossedLowerBand = true;
            }

            if (candles[i].Mid_O < bollingerBands[i].UpperBand && candles[i].Mid_C > bollingerBands[i].UpperBand)
            {
                crossedUpperBand = true;
            }

            if (crossedLowerBand)
            {
                higherLows = candles[i].Mid_C > latestLow;

                lowerLows = candles[i].Mid_C < latestLow;

                latestLow = candles[i].Mid_C;
            }

            if (crossedUpperBand)
            {
                higherHighs = candles[i].Mid_C > latestHigh;

                lowerHighs = candles[i].Mid_C < latestHigh;

                latestHigh = candles[i].Mid_C;
            }

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when crossedUpperBand && higherHighs && higherLows &&
                                candle.Mid_L > emaResult[i] &&
                                candle.Spread <= maxSpread => Signal.Buy,
                var candle when crossedLowerBand && lowerHighs && lowerLows &&
                                candle.Mid_H < emaResult[i] &&
                                candle.Spread <= maxSpread => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);

            if (crossedLowerBand)
            {
                crossedLowerBand = false;
            }

            if (crossedUpperBand)
            {
                crossedUpperBand = false;
            }
        }

        return result;
    }
}