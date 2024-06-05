namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcBollingerRsiEma(this Candle[] candles, int bbWindow = 20, int emaWindow = 100,
        double stdDev = 2, double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaResult = prices.CalcEma(emaWindow).ToArray();

        var bollingerBands = candles.CalcBollingerBands(bbWindow, stdDev);

        var stochRsi = candles.CalcStochRsi();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        var crossedLowerBand = false;

        var crossedUpperBand = false;

        //var higherHigh = false;

        //var higherLow = false;

        //var lowerHigh = false;

        //var lowerLow = false;

        //var latestHigh = double.MinValue;

        //var latestLow = double.MinValue;

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = minGain;

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when crossedLowerBand &&
                                candle.Direction == 1 &&
                                candle.Mid_L > emaResult[i] &&
                                stochRsi[i].KOscillator > stochRsi[i - 1].KOscillator &&
                                candle.Spread <= maxSpread => Signal.Buy,
                var candle when crossedUpperBand &&
                                candle.Direction == -1 &&
                                candle.Mid_H < emaResult[i] &&
                                stochRsi[i].KOscillator < stochRsi[i - 1].KOscillator &&
                                candle.Spread <= maxSpread => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);

            crossedLowerBand = candles[i].Mid_C < bollingerBands[i].LowerBand;

            crossedUpperBand = candles[i].Mid_C > bollingerBands[i].UpperBand;

            //if (crossedLowerBand)
            //{
            //    higherLow = candles[i].Mid_C > latestLow;

            //    lowerLow = candles[i].Mid_C < latestLow;

            //    latestLow = candles[i].Mid_C;
            //}

            //if (crossedUpperBand)
            //{
            //    higherHigh = candles[i].Mid_C > latestHigh;

            //    lowerHigh = candles[i].Mid_C < latestHigh;

            //    latestHigh = candles[i].Mid_C;
            //}
        }

        return result;
    }
}