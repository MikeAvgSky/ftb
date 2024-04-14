namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static Signal[] CalcTrend(this Candle[] candles, int shortEma = 8, int longEma = 21,
        int rsiWindow = 14, double priceLine = 50.0)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var shortEmaResult = prices.CalcEma(shortEma).ToArray();

        var longEmaResult = prices.CalcEma(longEma).ToArray();

        var rsiResult = candles.CalcRsi(rsiWindow);

        var length = candles.Length;

        var result = new Signal[length];

        for (var i = 0; i < length; i++)
        {
            if (shortEmaResult[i] > longEmaResult[i] &&
                rsiResult[i].Rsi > priceLine)
            {
                result[i] = Signal.Buy;
            }
            else if (shortEmaResult[i] < longEmaResult[i] &&
                     rsiResult[i].Rsi < priceLine)
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