namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static StochasticResult[] CalcStochastic(this Candle[] candles, int window = 14, int smoothK = 1, int smoothD = 3)
    {
        var length = candles.Length;

        var result = new StochasticResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new StochasticResult();

            if (i < window - 1) continue;

            result[i].Candle = candles[i];

            var lastCandles = new Candle[window];

            Array.Copy(candles[..(i + 1)], i - (window - 1), lastCandles, 0, window);

            var highestPrice = lastCandles.Select(c => c.Mid_C).Max();

            var lowestPrice = lastCandles.Select(c => c.Mid_C).Min();

            result[i].KOscillator = highestPrice - lowestPrice != 0
                ? 100 * (result[i].Candle.Mid_C - lowestPrice) / (highestPrice - lowestPrice)
                : 0.0;
        }

        if (smoothK > 1)
        {
            var kOscillators = result.Select(r => r.KOscillator).ToArray();

            var smaK = kOscillators.CalcSma(smoothK).ToArray();

            for (var i = 0; i < length; i++)
            {
                if (i < smoothK - 1)
                {
                    result[i].KOscillator = 0.0;

                    continue;
                }

                result[i].KOscillator = smaK[i];
            }
        }

        var oscillators = result.Select(r => r.KOscillator).ToArray();

        var smaD = oscillators.CalcSma(smoothD).ToArray();

        for (var i = 0; i < length; i++)
        {
            if (i < smoothD - 1)
            {
                result[i].DOscillator = 0.0;

                continue;
            }

            result[i].DOscillator = smaD[i];
        }

        return result;
    }
}