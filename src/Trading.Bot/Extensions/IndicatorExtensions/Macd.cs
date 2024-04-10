namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static MacdResult[] CalcMacd(this Candle[] candles, int shortWindow = 12, int longWindow = 26, int signal = 9)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaShort = prices.CalcEma(shortWindow).ToArray();

        var emaLong = prices.CalcEma(longWindow).ToArray();

        var length = candles.Length;

        var result = new MacdResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new MacdResult();

            result[i].Candle = candles[i];

            result[i].Macd = emaShort[i] - emaLong[i];
        }

        var ema = result.Select(m => m.Macd).ToArray().CalcEma(signal).ToArray();

        for (var i = 0; i < length; i++)
        {
            result[i].SignalLine = ema[i];

            result[i].Histogram = result[i].Macd - result[i].SignalLine;
        }

        return result;
    }
}