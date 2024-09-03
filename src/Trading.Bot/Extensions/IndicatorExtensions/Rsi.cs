namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static RsiResult[] CalcRsi(this Candle[] candles, int window = 14)
    {
        var length = candles.Length;

        var gains = new double[length];

        var losses = new double[length];

        var lastValue = 0.0;

        for (var i = 0; i < length; i++)
        {
            if (i == 0)
            {
                gains[i] = 0.0;

                losses[i] = 0.0;

                lastValue = candles[i].Mid_C;

                continue;
            }

            gains[i] = candles[i].Mid_C > lastValue ? candles[i].Mid_C - lastValue : 0.0;

            losses[i] = candles[i].Mid_C < lastValue ? lastValue - candles[i].Mid_C : 0.0;

            lastValue = candles[i].Mid_C;
        }

        var gains_rma = gains.CalcRma(window).ToArray();

        var losses_rma = losses.CalcRma(window).ToArray();

        var result = new RsiResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new RsiResult();

            result[i].Candle = candles[i];

            result[i].AverageGain = gains_rma[i];

            result[i].AverageLoss = losses_rma[i];

            if (i > 0)
            {
                var rs = result[i].AverageGain / result[i].AverageLoss;

                result[i].Rsi = 100.0 - 100.0 / (1.0 + rs);
            }
            else
            {
                result[i].Rsi = 0.0;
            }
        }

        return result;
    }
}