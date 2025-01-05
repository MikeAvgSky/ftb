namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcMikeStrategy(this Candle[] candles,
        int window = 20, double multiplier = 20, double minWidth = 0.0015,
        double rsiLow = 30, double rsiHigh = 70, double maxSpread = 0.0003,
        double gain = 0, double riskReward = 1)
    {
        var keltnerChannels = candles.CalcKeltnerChannels(window, multiplier: multiplier);

        var rsi = candles.CalcRsi();

        var atr = candles.CalcAtr();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var crossedLowerBand = candles[i].Mid_O > keltnerChannels[i].LowerBand && candles[i].Mid_C < keltnerChannels[i].LowerBand;

            var crossedUpperBand = candles[i].Mid_O < keltnerChannels[i].UpperBand && candles[i].Mid_C > keltnerChannels[i].UpperBand;

            var bandWidth = keltnerChannels[i].UpperBand - keltnerChannels[i].LowerBand;

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when crossedLowerBand &&
                                bandWidth > minWidth &&
                                rsi[i].Rsi < rsiLow &&
                                candle.Spread <= maxSpread => Signal.Buy,
                var candle when crossedUpperBand &&
                                bandWidth > minWidth &&
                                rsi[i].Rsi > rsiHigh &&
                                candle.Spread <= maxSpread => Signal.Sell,
                _ => Signal.None
            };

            result[i].Gain = gain != 0 ? gain : Math.Round(atr[i].Atr * 2, 5);

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}