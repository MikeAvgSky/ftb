namespace Trading.Bot.Extensions.IndicatorExtensions;

public static partial class Indicator
{
    public static IndicatorResult[] CalcMikeStrategy(this Candle[] candles, int shortWindow, int longWindow,
        double stdDev = 1.5, decimal maxSpread = 0.0004m, decimal minGain = 0.002m, decimal riskReward = 1.5m)
    {
        var macd = candles.CalcMacd();

        var bollingerBands = candles.CalcBollingerBands(shortWindow, stdDev);

        var prices = candles.Select(c => (double)(c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var shortEma = prices.CalcEma(shortWindow).ToArray();

        var longEma = prices.CalcEma(longWindow).ToArray();

        var length = candles.Length;

        var result = new IndicatorResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] ??= new IndicatorResult();

            result[i].Candle = candles[i];

            var bullishTrend = shortEma[i] > longEma[i];

            var emaRising = i > 0 && shortEma[i] > shortEma[i - 1];

            var macdRising = i > 0 && macd[i].Macd > macd[i - 1].Macd;

            var bearishTrend = shortEma[i] < longEma[i];

            var emaFalling = i > 0 && shortEma[i] < shortEma[i - 1];

            var macdFalling = i > 0 && macd[i].Macd < macd[i - 1].Macd;

            var macdDelta = macd[i].Macd - macd[i].SignalLine;

            var crossedLowerBand = candles[i].Mid_O > (decimal)bollingerBands[i].LowerBand &&
                                   candles[i].Mid_C < (decimal)bollingerBands[i].LowerBand;

            var crossedUpperBand = candles[i].Mid_O < (decimal)bollingerBands[i].UpperBand &&
                                   candles[i].Mid_C > (decimal)bollingerBands[i].UpperBand;

            result[i].Signal = i < longWindow ? Signal.None : macdDelta switch
            {
                > 0 when bullishTrend && emaRising && macdRising &&
                         crossedUpperBand && candles[i].BodyPercentage > 50 &&
                         candles[i].Spread <= maxSpread => Signal.Buy,
                < 0 when bearishTrend && emaFalling && macdFalling &&
                         crossedLowerBand && candles[i].BodyPercentage > 50 &&
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