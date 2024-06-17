namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static Signal[] CalcEmaTrend(this Candle[] candles, int emaWindow = 150)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaResult = prices.CalcEma(emaWindow).ToArray();

        var length = candles.Length;

        var result = new Signal[length];

        for (var i = 0; i < length; i++)
        {
            if (candles[i].Mid_L > emaResult[i])
            {
                result[i] = Signal.Buy;
            }
            else if (candles[i].Mid_H < emaResult[i])
            {
                result[i] = Signal.Sell;
            }
            else
            {
                result[i] = Signal.None;
            }
        }

        return result;
    }
}