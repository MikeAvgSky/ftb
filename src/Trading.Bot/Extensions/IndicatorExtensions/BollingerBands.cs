namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static BollingerBandsResult[] CalcBollingerBands(this Candle[] candles, int window = 20, double stdDev = 2)
    {
        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var rolStdDev = typicalPrice.CalcRolStdDev(window, stdDev).ToArray();

        var sma = typicalPrice.CalcSma(window).ToArray();

        var length = candles.Length;

        var result = new BollingerBandsResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new BollingerBandsResult();

            result[i].Candle = candles[i];

            result[i].Sma = sma[i];

            result[i].UpperBand = sma[i] + rolStdDev[i] * stdDev;

            result[i].LowerBand = sma[i] - rolStdDev[i] * stdDev;
        }

        return result;
    }
}