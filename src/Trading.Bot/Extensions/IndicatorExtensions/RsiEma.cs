namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcRsiEma(this Candle[] candles, int rsiWindow = 14, int emaWindow = 200, double rsiLimit = 50.0,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var rsiResult = candles.CalcRsi(rsiWindow);

        var prices = candles.Select(c => c.Mid_C).ToArray();

        var emaResult = prices.CalcEma(emaWindow).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var rsi = rsiResult[i].Rsi;

            var ema = emaResult[i];

            var engulfing = i > 0 && candles[i].IsEngulfingCandle(candles[i - 1]);

            result[i].Gain = Math.Abs(candles[i].Mid_C - ema);

            result[i].Signal = engulfing switch
            {
                true when candles[i].Direction == 1 &&
                          candles[i].Mid_L > ema &&
                          rsi > rsiLimit &&
                          candles[i].Spread <= maxSpread &&
                          result[i].Gain >= minGain => Signal.Buy,
                true when candles[i].Direction == -1 &&
                          candles[i].Mid_H < ema &&
                          rsi < rsiLimit &&
                          candles[i].Spread <= maxSpread &&
                          result[i].Gain >= minGain => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i]);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}