namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static Signal[] CalcTrend(this Candle[] candles, int shortEma = 8, int longEma = 21, int longSma = 55,
        int rsiWindow = 14, double rsiLower = 30, double rsiUpper = 70)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var shortEmaResult = prices.CalcEma(shortEma).ToArray();

        var longEmaResult = prices.CalcEma(longEma).ToArray();

        var longSmaResult = prices.CalcEma(longSma).ToArray();

        var rsiResult = candles.CalcRsi(rsiWindow);

        var length = candles.Length;

        var result = new Signal[length];

        for (var i = 0; i < length; i++)
        {
            if (shortEmaResult[i] < longEmaResult[i] &&
                longEmaResult[i] < longSmaResult[i] &&
                rsiResult[i].Rsi < rsiLower)
            {
                result[i] = Signal.Sell;
            }
            else if (shortEmaResult[i] > longEmaResult[i] &&
                     longEmaResult[i] > longSmaResult[i] &&
                     rsiResult[i].Rsi > rsiUpper)
            {
                result[i] = Signal.Buy;
            }
            else
            {
                result[i] = Signal.None;
            }
        }

        return result;
    }
}