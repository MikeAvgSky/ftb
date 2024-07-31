namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcMeanReversion(this Candle[] candles, int bbWindow = 20, int emaWindow = 100,
        double stdDev = 2, double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaResult = prices.CalcEma(emaWindow).ToArray();

        var bollingerBands = candles.CalcBollingerBands(bbWindow, stdDev);

        var rsiResult = candles.CalcRsi();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = bollingerBands[i].UpperBand - bollingerBands[i].LowerBand;

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when candles[i].Mid_O > bollingerBands[i].LowerBand &&
                                candles[i].Mid_C < bollingerBands[i].LowerBand &&
                                bollingerBands[i].LowerBand < emaResult[i] &&
                                bollingerBands[i].UpperBand > emaResult[i] &&
                                rsiResult[i].Rsi <= 30 &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Buy,
                var candle when candles[i].Mid_O < bollingerBands[i].UpperBand &&
                                candles[i].Mid_C > bollingerBands[i].UpperBand &&
                                bollingerBands[i].LowerBand < emaResult[i] &&
                                bollingerBands[i].UpperBand > emaResult[i] &&
                                rsiResult[i].Rsi >= 70 &&
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