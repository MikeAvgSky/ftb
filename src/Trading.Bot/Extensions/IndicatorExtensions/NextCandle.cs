namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcNextCandle(this Candle[] candles, double maxSpread = 0.0003,
        double minGain = 0.0006, double riskReward = 1)
    {
        var macd = candles.CalcMacd(5, 12, 5);

        var rsi = candles.CalcRsi(9);

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var macdFalling = i != 0 && Math.Round(macd[i].Macd, 5) < Math.Round(macd[i - 1].Macd, 5);

            var signalFalling = i != 0 && Math.Round(macd[i].SignalLine, 5) < Math.Round(macd[i - 1].SignalLine, 5);

            var macdRising = i != 0 && Math.Round(macd[i].Macd, 5) > Math.Round(macd[i - 1].Macd, 5);

            var signalRising = i != 0 && Math.Round(macd[i].SignalLine, 5) > Math.Round(macd[i - 1].SignalLine, 5);

            result[i].Signal = rsi[i].Rsi switch
            {
                < 55 when macdRising && signalRising &&
                          candles[i].Spread <= maxSpread => Signal.Buy,
                > 45 when macdFalling && signalFalling &&
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