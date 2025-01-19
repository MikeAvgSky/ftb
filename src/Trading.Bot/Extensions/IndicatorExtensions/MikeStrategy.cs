namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcMikeStrategy(this Candle[] candles,
        int window, double maxSpread = 0.0004, double minGain = 0.002, double riskReward = 1)
    {
        var macd = candles.CalcMacd();

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var ema = prices.CalcEma(window).ToArray();

        var sma = prices.CalcSma(window).ToArray();

        var resistance = prices.CalcEma(window * 2).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var truncatedSma = Math.Floor(sma[i] * 10000) / 10000;

            var truncatedSmaPrev = i == 0 ? 0.0 : Math.Floor(sma[i - 1] * 10000) / 10000;

            var smaRising = i != 0 && truncatedSma > truncatedSmaPrev;

            var smaFalling = i != 0 && truncatedSma < truncatedSmaPrev;

            var macDelta = Math.Floor(macd[i].Macd * 10000) / 10000 - Math.Floor(macd[i].SignalLine * 10000) / 10000;

            result[i].Gain = Math.Abs(candles[i].Mid_C - resistance[i]);

            result[i].Signal = i < window ? Signal.None : macDelta switch
            {
                > 0 when macd[i].Macd > 0 &&
                         smaRising && ema[i] > sma[i] &&
                         candles[i].Spread <= maxSpread &&
                         result[i].Gain >= minGain => Signal.Buy,
                < 0 when macd[i].Macd < 0 &&
                         smaFalling && ema[i] < sma[i] &&
                         candles[i].Spread <= maxSpread &&
                         result[i].Gain >= minGain => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}