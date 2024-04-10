namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static AtrResult[] CalcAtr(this Candle[] candles, int window = 14)
    {
        var length = candles.Length;

        var result = new AtrResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new AtrResult();

            var prevMidC = i == 0 ? candles[i].Mid_C : candles[i - 1].Mid_C;

            var tr1 = candles[i].Mid_H - candles[i].Mid_L;

            var tr2 = Math.Abs(candles[i].Mid_H - prevMidC);

            var tr3 = Math.Abs(prevMidC - candles[i].Mid_L);

            var trueRanges = new[] { tr1, tr2, tr3 };

            result[i].Candle = candles[i];

            result[i].MaxTr = trueRanges.Max();
        }

        var maxTra = result.Select(x => x.MaxTr).ToArray().CalcSma(window).ToArray();

        for (var i = 0; i < length; i++)
        {
            result[i].Atr = maxTra[i];
        }

        return result;
    }
}