namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static StochasticResult[] CalcStochRsi(this Candle[] candles, int window = 14, int smoothK = 3, int smoothD = 3)
    {
        var rsiResult = candles.CalcRsi(window).Select(r => r.Rsi).ToArray();

        var length = rsiResult.Length;

        var result = new StochasticResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new StochasticResult();

            if (i < window - 1) continue;

            var lastRsiValues = new double[window];

            Array.Copy(rsiResult[..(i + 1)], i - (window - 1),
                lastRsiValues, 0, window);

            var highestRsi = lastRsiValues.Max();

            var lowestRsi = lastRsiValues.Min();

            result[i].KOscillator = highestRsi - lowestRsi != 0
                ? 100 * (rsiResult[i] - lowestRsi) / (highestRsi - lowestRsi)
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