namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static KeltnerChannelsResult[] CalcKeltnerChannels(this Candle[] candles, int emaWindow = 20, int atrWindow = 10)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var ema = prices.CalcEma(emaWindow).ToArray();

        var atr = candles.CalcAtr(atrWindow).ToArray();

        var length = candles.Length;

        var result = new KeltnerChannelsResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new KeltnerChannelsResult();

            result[i].Candle = candles[i];

            result[i].Ema = ema[i];

            result[i].UpperBand = atr[i].Atr * 2 + ema[i];

            result[i].LowerBand = ema[i] - atr[i].Atr * 2;
        }

        return result;
    }
}