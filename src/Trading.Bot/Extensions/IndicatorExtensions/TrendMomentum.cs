namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcTrendMomentum(this Candle[] candles, int bbWindow = 20, int emaWindow = 100,
        double stdDev = 2, double rsiLower = 30, double rsiUpper = 70, double maxSpread = 0.0004, double gain = 0,
        double riskReward = 1)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaResult = prices.CalcEma(emaWindow).ToArray();

        var bollingerBands = candles.CalcBollingerBands(bbWindow, stdDev);

        var rsiResult = candles.CalcRsi();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        var higherHighs = false;

        var lowerLows = false;

        var latestHigh = candles[0].Mid_C;

        var latestLow = candles[0].Mid_C;

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = gain != 0 ? gain : Math.Abs(candles[i].Mid_C - bollingerBands[i].Sma);

            var crossedLowerBand = candles[i].Mid_O > bollingerBands[i].LowerBand && candles[i].Mid_C < bollingerBands[i].LowerBand;

            var crossedUpperBand = candles[i].Mid_O < bollingerBands[i].UpperBand && candles[i].Mid_C > bollingerBands[i].UpperBand;

            if (crossedLowerBand)
            {
                lowerLows = candles[i].Mid_C < latestLow;

                latestLow = candles[i].Mid_C;
            }

            if (crossedUpperBand)
            {
                higherHighs = candles[i].Mid_C > latestHigh;

                latestHigh = candles[i].Mid_C;
            }

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when crossedUpperBand && higherHighs &&
                                bollingerBands[i].LowerBand > emaResult[i] &&
                                rsiResult[i].Rsi > rsiUpper &&
                                candle.Spread <= maxSpread => Signal.Buy,
                var candle when crossedLowerBand && lowerLows &&
                                bollingerBands[i].UpperBand < emaResult[i] &&
                                rsiResult[i].Rsi < rsiLower &&
                                candle.Spread <= maxSpread => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}