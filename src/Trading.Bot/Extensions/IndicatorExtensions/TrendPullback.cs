namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcTrendPullback(this Candle[] candles, int bbWindow = 20, int emaWindow = 100,
        double stdDev = 2, double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaResult = prices.CalcEma(emaWindow).ToArray();

        var bollingerBands = candles.CalcBollingerBands(bbWindow, stdDev);

        var macd = candles.CalcMacd();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        var crossedLowerBand = false;

        var crossedUpperBand = false;

        var lastCrossedLowerBandIndex = 0;

        var lastCrossedUpperBandIndex = 0;

        var higherHighs = false;

        var lowerLows = false;

        var latestHigh = candles[0].Mid_C;

        var latestLow = candles[0].Mid_C;

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = minGain;

            var rising = crossedLowerBand && macd[i].Histogram > macd[i - 1].Histogram;

            var falling = crossedUpperBand && macd[i].Histogram < macd[i - 1].Histogram;

            if (crossedLowerBand && rising)
            {
                var lastCrossedLowerBandCandle = candles[lastCrossedLowerBandIndex];

                lowerLows = lastCrossedLowerBandCandle.Mid_C < latestLow;

                latestLow = lastCrossedLowerBandCandle.Mid_C;
            }

            if (crossedUpperBand && falling)
            {
                var lastCrossedUpperBandCandle = candles[lastCrossedUpperBandIndex];

                higherHighs = lastCrossedUpperBandCandle.Mid_C > latestHigh;

                latestHigh = lastCrossedUpperBandCandle.Mid_C;
            }

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when crossedLowerBand && rising && higherHighs &&
                                candle.Direction == 1 && !lowerLows &&
                                bollingerBands[i].LowerBand > emaResult[i] &&
                                candle.Spread <= maxSpread => Signal.Buy,
                var candle when crossedUpperBand && falling && lowerLows &&
                                candle.Direction == -1 && !higherHighs &&
                                bollingerBands[i].UpperBand < emaResult[i] &&
                                candle.Spread <= maxSpread => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);

            if (crossedLowerBand && rising)
            {
                crossedLowerBand = false;
            }

            if (crossedUpperBand && falling)
            {
                crossedUpperBand = false;
            }

            if (candles[i].Mid_C < bollingerBands[i].LowerBand)
            {
                crossedLowerBand = true;
                lastCrossedLowerBandIndex = i;
            }

            if (candles[i].Mid_C > bollingerBands[i].UpperBand)
            {
                crossedUpperBand = true;
                lastCrossedUpperBandIndex = i;
            }
        }

        return result;
    }
}