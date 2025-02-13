namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcMikeStrategy(this Candle[] candles, int shortWindow, int longWindow,
        int priceActionWindow, decimal maxSpread = 0.0004m, decimal minGain = 0.002m, decimal riskReward = 1)
    {
        var macd = candles.CalcMacd();

        var prices = candles.Select(c => (double)c.Mid_C).ToArray();

        var shortEma = prices.CalcEma(shortWindow).ToArray();

        var longEma = prices.CalcSma(longWindow).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var bullishTrend = shortEma[i] > longEma[i];

            var emaRising = i > 0 && shortEma[i] > longEma[i - 1];

            var macdRising = i > 0 && macd[i].Macd > macd[i - 1].Macd;

            var bearishTrend = shortEma[i] < longEma[i];

            var emaFalling = i > 0 && shortEma[i] < longEma[i - 1];

            var macdFalling = i > 0 && macd[i].Macd < macd[i - 1].Macd;

            var macdDelta = macd[i].Macd - macd[i].SignalLine;

            result[i].Signal = i < priceActionWindow ? Signal.None : macdDelta switch
            {
                > 0 when bullishTrend && emaRising && macdRising &&
                         candles[(i - priceActionWindow)..i].HigherHighs() &&
                         candles[(i - priceActionWindow)..i].HigherLows() &&
                         candles[i].Spread <= maxSpread => Signal.Buy,
                < 0 when bearishTrend && emaFalling && macdFalling &&
                         candles[(i - priceActionWindow)..i].LowerHighs() &&
                         candles[(i - priceActionWindow)..i].LowerLows() &&
                         candles[i].Spread <= maxSpread => Signal.Sell,
                _ => Signal.None
            };

            result[i].Gain = minGain;

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}