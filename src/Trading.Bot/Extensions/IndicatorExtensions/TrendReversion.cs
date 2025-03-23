namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcTrendReversion(this Candle[] candles, int shortWindow, int longWindow,
        double stdDev = 2, decimal maxSpread = 0.0004m, decimal minGain = 0.002m, decimal riskReward = 1.5m)
    {
        var bollingerBands = candles.CalcBollingerBands(shortWindow, stdDev);

        var prices = candles.Select(c => (double)c.Mid_C).ToArray();

        var shortEma = prices.CalcEma(shortWindow).ToArray();

        var longEma = prices.CalcEma(longWindow).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var trend = 0;

            if (shortEma[i] > longEma[i]) trend = 1;

            if (shortEma[i] < longEma[i]) trend = -1;

            result[i].Signal = i < longWindow ? Signal.None : trend switch
            {
                > 0 when candles[i].Mid_O < (decimal)bollingerBands[i].LowerBand &&
                         candles[i].Mid_C > (decimal)bollingerBands[i].LowerBand &&
                         candles[i].BodyPercentage > 50 &&
                         candles[i].Spread <= maxSpread => Signal.Buy,
                < 0 when candles[i].Mid_O > (decimal)bollingerBands[i].UpperBand &&
                         candles[i].Mid_C < (decimal)bollingerBands[i].UpperBand &&
                         candles[i].BodyPercentage > 50 &&
                         candles[i].Spread <= maxSpread => Signal.Sell,
                _ => Signal.None
            };

            result[i].Gain = minGain;

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}