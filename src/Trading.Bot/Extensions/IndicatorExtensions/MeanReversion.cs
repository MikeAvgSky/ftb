namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcMeanReversion(this Candle[] candles, int window = 20, double stdDev = 2,
        double rsiLower = 30, double rsiUpper = 70, double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var bollingerBands = candles.CalcBollingerBands(window, stdDev);

        var rsiResult = candles.CalcRsi();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        var higherLows = false;

        var lowerHighs = false;

        var latestHigh = candles[0].Mid_C;

        var latestLow = candles[0].Mid_C;

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = bollingerBands[i].UpperBand - bollingerBands[i].LowerBand;

            var crossedLowerBand = candles[i].Mid_O > bollingerBands[i].LowerBand && candles[i].Mid_C < bollingerBands[i].LowerBand;

            var crossedUpperBand = candles[i].Mid_O < bollingerBands[i].UpperBand && candles[i].Mid_C > bollingerBands[i].UpperBand;

            if (crossedLowerBand)
            {
                higherLows = candles[i].Mid_C > latestLow;

                latestLow = candles[i].Mid_C;
            }

            if (crossedUpperBand)
            {
                lowerHighs = candles[i].Mid_C < latestHigh;

                latestHigh = candles[i].Mid_C;
            }

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when crossedLowerBand && higherLows &&
                                rsiResult[i].Rsi <= rsiLower &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Buy,
                var candle when crossedUpperBand && lowerHighs &&
                                rsiResult[i].Rsi >= rsiUpper &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}