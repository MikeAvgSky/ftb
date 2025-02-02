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

            var maDelta = ema[i].Round(5) - sma[i].Round(5);

            var maDeltaPrev = i > 0 ? ema[i - 1].Round(5) - sma[i - 1].Round(5) : 0;

            var emaRising = i > 0 && ema[i].Round(5) > ema[i - 1].Round(5);

            var emaFalling = i > 0 && ema[i].Round(5) < ema[i - 1].Round(5);

            var macdDelta = macd[i].Macd.Round(5) - macd[i].SignalLine.Round(5);

            result[i].Signal = i < window ? Signal.None : maDelta switch
            {
                > 0 when maDeltaPrev <= 0 && emaRising &&
                         macdDelta > 0 && rsi[i].Rsi > 50 &&
                         candles[i].Spread <= maxSpread => Signal.Buy,
                < 0 when maDeltaPrev >= 0 && emaFalling &&
                         macdDelta < 0 && rsi[i].Rsi < 50 &&
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