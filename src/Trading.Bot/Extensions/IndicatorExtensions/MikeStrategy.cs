namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcMikeStrategy(this Candle[] candles,
        int window = 20, double stDev = 2, int emaMedium = 20, int smaLong = 50,
        double maxSpread = 0.0003, double minGain = 0.001, double riskReward = 1)
    {
        var bollingerBands = candles.CalcBollingerBands(window, stDev);

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var medEma = prices.CalcEma(emaMedium).ToArray();

        var longSma = prices.CalcSma(smaLong).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = Math.Round(Math.Abs(candles[i].Mid_C - bollingerBands[i].Sma), 5);

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when candle.Mid_O > bollingerBands[i].LowerBand &&
                                candle.Mid_C < bollingerBands[i].LowerBand &&
                                medEma[i] > longSma[i] && result[i].Gain >= minGain &&
                                candle.Spread <= maxSpread => Signal.Buy,
                var candle when candle.Mid_O < bollingerBands[i].LowerBand &&
                                candle.Mid_C > bollingerBands[i].LowerBand &&
                                medEma[i] < longSma[i] && result[i].Gain >= minGain &&
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