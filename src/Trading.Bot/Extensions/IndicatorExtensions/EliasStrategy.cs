namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcEliasStrategy(this Candle[] candles, int emaShort = 8,
        int emaMedium = 21, int smaLong = 50, int emaLong = 100, decimal minGain = 0.002m,
        decimal riskReward = 1, decimal maxSpread = 0.0003m)
    {
        var macd = candles.CalcMacd();

        var prices = candles.Select(c => (double)c.Mid_C).ToArray();

        var shortEma = prices.CalcEma(emaShort).ToArray();

        var medEma = prices.CalcEma(emaMedium).ToArray();

        var longSma = prices.CalcSma(smaLong).ToArray();

        var resistance = prices.CalcEma(emaLong).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var macDelta = macd[i].Macd - macd[i].SignalLine;

            result[i].Gain = minGain;

            result[i].Signal = macDelta switch
            {
                > 0 when macd[i].Macd > 0 && candles[i].Mid_C > (decimal)resistance[i] &&
                         shortEma[i] > medEma[i] && medEma[i] > longSma[i] &&
                         candles[i].Spread <= maxSpread => Signal.Buy,
                < 0 when macd[i].Macd < 0 && candles[i].Mid_C < (decimal)resistance[i] &&
                         shortEma[i] < medEma[i] && medEma[i] < longSma[i] &&
                         candles[i].Spread <= maxSpread => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}