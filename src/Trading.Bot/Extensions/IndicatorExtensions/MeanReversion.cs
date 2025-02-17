namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcMeanReversion(this Candle[] candles, int window = 20, double stdDev = 2,
        double rsiLower = 30, double rsiUpper = 70, decimal maxSpread = 0.0004m, decimal gain = 0, decimal riskReward = 1)
    {
        var bollingerBands = candles.CalcBollingerBands(window, stdDev);

        var rsiResult = candles.CalcRsi();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        var higherLows = false;

        var lowerHighs = false;

        var latestHigh = candles[0].Mid_C;

        var latestLow = candles[0].Mid_C;

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            result[i].Gain = gain != 0 ? gain : Math.Abs(candles[i].Mid_C - (decimal)bollingerBands[i].Sma);

            var crossedLowerBand = candles[i].Mid_O > (decimal)bollingerBands[i].LowerBand && candles[i].Mid_C < (decimal)bollingerBands[i].LowerBand;

            var crossedUpperBand = candles[i].Mid_O < (decimal)bollingerBands[i].UpperBand && candles[i].Mid_C > (decimal)bollingerBands[i].UpperBand;

            if (crossedLowerBand && rsiResult[i].Rsi < rsiLower)
            {
                higherLows = candles[i].Mid_C > latestLow;

                latestLow = candles[i].Mid_C;
            }

            if (crossedUpperBand && rsiResult[i].Rsi > rsiUpper)
            {
                lowerHighs = candles[i].Mid_C < latestHigh;

                latestHigh = candles[i].Mid_C;
            }

            result[i].Signal = i == 0 ? Signal.None : candles[i] switch
            {
                var candle when crossedLowerBand && higherLows &&
                                rsiResult[i].Rsi < rsiLower &&
                                candle.Spread <= maxSpread => Signal.Buy,
                var candle when crossedUpperBand && lowerHighs &&
                                rsiResult[i].Rsi > rsiUpper &&
                                candle.Spread <= maxSpread => Signal.Sell,
                _ => Signal.None
            };

            result[i].TakeProfit = candles[i].CalcTakeProfit(result[i], riskReward);

            result[i].StopLoss = candles[i].CalcStopLoss(result[i]);

            result[i].Loss = Math.Abs(candles[i].Mid_C - result[i].StopLoss);
        }

        return result;
    }
}