namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcMikeStrategy(this Candle[] candles,
        int window, double maxSpread = 0.0004, double minGain = 0.001, double riskReward = 1)
    {
        var macd = candles.CalcMacd();

        var rsi = candles.CalcRsi();

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var ema = prices.CalcEma(window).ToArray();

        var sma = prices.CalcSma(window).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var emaRising = i > 0 && ema[i] > ema[i - 1];

            var macdRising = i > 0 && macd[i].Macd > macd[i - 1].Macd;

            var bullishTrend = emaRising && macdRising && ema[i] > sma[i];

            var emaFalling = i > 0 && ema[i] < ema[i - 1];

            var macdFalling = i > 0 && macd[i].Macd < macd[i - 1].Macd;

            var bearishTrend = emaFalling && macdFalling && ema[i] < sma[i];

            var macdDelta = macd[i].Macd - macd[i].SignalLine;

            result[i].Signal = i < window ? Signal.None : macdDelta switch
            {
                > 0 when bullishTrend && rsi[i].Rsi > 50 &&
                         candles[(i - window)..i].HigherHighs() &&
                         candles[(i - window)..i].HigherLows() &&
                         candles[i].Spread <= maxSpread => Signal.Buy,
                < 0 when bearishTrend && rsi[i].Rsi < 50 &&
                         candles[(i - window)..i].LowerHighs() &&
                         candles[(i - window)..i].LowerLows() &&
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