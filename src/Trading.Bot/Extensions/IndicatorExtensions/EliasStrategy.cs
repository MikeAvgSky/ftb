namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcEliasStrategy(this Candle[] candles, int emaShort = 8,
        int emaMedium = 21, int smaLong = 50, int stopLossWindow = 100, int macdShort = 12,
        int macdLong = 26, double riskReward = 1, double minGain = 0.002, double maxSpread = 0.0003)
    {
        var macd = candles.CalcMacd(macdShort, macdLong);

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var shortEma = prices.CalcEma(emaShort).ToArray();

        var medEma = prices.CalcEma(emaMedium).ToArray();

        var longSma = prices.CalcSma(smaLong).ToArray();

        var resistance = prices.CalcEma(stopLossWindow).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var macDelta = macd[i].Macd - macd[i].SignalLine;

            result[i].Gain = Math.Abs(candles[i].Mid_C - resistance[i]);

            result[i].Signal = macDelta switch
            {
                > 0 when macd[i].Macd > 0 && candles[i].Mid_C > resistance[i] &&
                         shortEma[i] > medEma[i] && medEma[i] > longSma[i] &&
                         candles[i].Spread <= maxSpread && result[i].Gain >= minGain => Signal.Buy,
                < 0 when macd[i].Macd < 0 && candles[i].Mid_C < resistance[i] &&
                         shortEma[i] < medEma[i] && medEma[i] < longSma[i] &&
                         candles[i].Spread <= maxSpread && result[i].Gain >= minGain => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}