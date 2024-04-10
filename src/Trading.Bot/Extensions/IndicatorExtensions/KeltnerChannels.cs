namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static KeltnerChannelsResult[] CalcKeltnerChannels(this Candle[] candles, int emaWindow = 20, int atrWindow = 10,
        double maxSpread = 0.0004, double minGain = 0.0006, double riskReward = 1.5)
    {
        var prices = candles.Select(c => c.Mid_C).ToArray();

        var ema = prices.CalcEma(emaWindow).ToArray();

        var atr = candles.CalcAtr(atrWindow).ToArray();

        var length = candles.Length;

        var result = new KeltnerChannelsResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new KeltnerChannelsResult();

            result[i].Candle = candles[i];

            result[i].Ema = ema[i];

            result[i].UpperBand = atr[i].Atr * 2 + ema[i];

            result[i].LowerBand = result[i].Ema - atr[i].Atr * 2;

            result[i].Gain = Math.Abs(candles[i].Mid_C - result[i].Ema);

            result[i].Signal = candles[i] switch
            {
                var candle when candle.Mid_C < result[i].LowerBand &&
                                candle.Mid_O > result[i].LowerBand &&
                                candle.Spread <= maxSpread &&
                                result[i].Gain >= minGain => Signal.Buy,
                var candle when candle.Mid_C > result[i].UpperBand &&
                                candle.Mid_O < result[i].UpperBand &&
                                candle.Spread <= maxSpread &&
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