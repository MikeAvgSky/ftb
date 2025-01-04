namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcEliasStrategy(this Candle[] candles, int emaShort = 8,
        int emaMedium = 21, int smaLong = 50, double riskReward = 1, double gain = 0)
    {
        var macd = candles.CalcMacd();

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var shortEma = prices.CalcEma(emaShort).ToArray();

        var medEma = prices.CalcEma(emaMedium).ToArray();

        var longSma = prices.CalcSma(smaLong).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var macDelta = macd[i].Macd - macd[i].SignalLine;

            result[i].Signal = macDelta switch
            {
                > 0 when macd[i].Macd > 0 && candles[i].Direction == -1 &&
                         shortEma[i] > medEma[i] &&
                         medEma[i] > longSma[i] => Signal.Buy,
                < 0 when macd[i].Macd < 0 && candles[i].Direction == 1 &&
                         shortEma[i] < medEma[i] &&
                         medEma[i] < longSma[i] => Signal.Sell,
                _ => Signal.None
            };

            result[i].Gain = gain != 0 ? gain : result[i].Signal switch
            {
                Signal.Buy => i > 6 ? Math.Abs(candles[i].Mid_C - candles[(i - 6)..i].Min(c => c.Mid_C)) : 0,
                Signal.Sell => i > 6 ? Math.Abs(candles[i].Mid_C - candles[(i - 6)..i].Max(c => c.Mid_C)) : 0,
                _ => 0
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}