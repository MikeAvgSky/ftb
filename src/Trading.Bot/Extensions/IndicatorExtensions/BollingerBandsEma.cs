namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcBollingerBandsEma(this Candle[] candles, int bbWindow = 20, int emaWindow = 100,
        double stdDev = 2, double maxSpread = 0.0004, double minGain = 0.0006, int minVolume = 100, double riskReward = 1.5)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaResult = prices.CalcEma(emaWindow).ToArray();

        var bollingerBands = candles.CalcBollingerBands(bbWindow, stdDev);

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = Math.Abs(candles[i].Mid_C - emaResult[i]);

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when candle.Mid_O < bollingerBands[i].Sma &&
                                candle.Mid_C > bollingerBands[i].Sma &&
                                emaResult[i] < bollingerBands[i].LowerBand &&
                                candle.Spread <= maxSpread &&
                                candle.Volume >= minVolume &&
                                result[i].Gain >= minGain => Signal.Buy,
                var candle when candle.Mid_O > bollingerBands[i].Sma &&
                                candle.Mid_C < bollingerBands[i].Sma &&
                                emaResult[i] > bollingerBands[i].UpperBand &&
                                candle.Spread <= maxSpread &&
                                candle.Volume >= minVolume &&
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