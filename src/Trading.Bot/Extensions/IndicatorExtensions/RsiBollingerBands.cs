namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcRsiBollingerBands(this Candle[] candles, int bbWindow = 20, int rsiWindow = 14, double stdDev = 2,
        double maxSpread = 0.0004, double minGain = 0.0006, int minVolume = 100, double riskReward = 1.5, double rsiLower = 30, double rsiUpper = 70,
        bool breakout = false)
    {
        var rsiResults = candles.CalcRsi(rsiWindow);

        var bollingerBands = candles.CalcBollingerBands(bbWindow, stdDev);

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = Math.Abs(candles[i].Mid_C - bollingerBands[i].Sma);

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when candle.Mid_C < bollingerBands[i].LowerBand &&
                                candle.Mid_O > bollingerBands[i].LowerBand &&
                                rsiResults[i].Rsi < rsiLower &&
                                candle.Spread <= maxSpread &&
                                candle.Volume >= minVolume &&
                                result[i].Gain >= minGain && !breakout => Signal.Buy,
                var candle when candle.Mid_C > bollingerBands[i].UpperBand &&
                                candle.Mid_O < bollingerBands[i].UpperBand &&
                                rsiResults[i].Rsi > rsiUpper &&
                                rsiResults[i].Rsi > rsiResults[i - 1].Rsi &&
                                candle.Spread <= maxSpread &&
                                candle.Volume >= minVolume &&
                                result[i].Gain >= minGain && breakout => Signal.Buy,
                var candle when candle.Mid_C > bollingerBands[i].UpperBand &&
                                candle.Mid_O < bollingerBands[i].UpperBand &&
                                rsiResults[i].Rsi > rsiUpper &&
                                candle.Spread <= maxSpread &&
                                candle.Volume >= minVolume &&
                                result[i].Gain >= minGain && !breakout => Signal.Sell,
                var candle when candle.Mid_C < bollingerBands[i].LowerBand &&
                                candle.Mid_O > bollingerBands[i].LowerBand &&
                                rsiResults[i].Rsi < rsiLower &&
                                rsiResults[i].Rsi < rsiResults[i - 1].Rsi &&
                                candle.Spread <= maxSpread &&
                                candle.Volume >= minVolume &&
                                result[i].Gain >= minGain && breakout => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i]);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i], riskReward);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}