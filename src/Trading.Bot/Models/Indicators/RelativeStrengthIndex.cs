namespace Trading.Bot.Models.Indicators;

public class RelativeStrengthIndex : Indicator
{
    public double AverageGain { get; set; }
    public double AverageLoss { get; set; }
    public double RSI { get; set; }

    public RelativeStrengthIndex(Candle candle)
    {
        Candle = candle;
    }

    public RelativeStrengthIndex() { }

    public static IEnumerable<RelativeStrengthIndex> ProcessCandles(IReadOnlyCollection<Candle> candles, TradeSettings settings)
    {
        var rsi = candles.Select(c => new RelativeStrengthIndex(c)).ToArray();

        var gains = new double[rsi.Length];

        var losses = new double[rsi.Length];

        var lastValue = 0.0;

        for (var i = 0; i < rsi.Length; i++)
        {
            if (i == 0)
            {
                gains[i] = 0.0;

                losses[i] = 0.0;

                lastValue = rsi[i].Candle.Mid_C;

                continue;
            }

            gains[i] = rsi[i].Candle.Mid_C > lastValue ? rsi[i].Candle.Mid_C - lastValue : 0.0;

            losses[i] = rsi[i].Candle.Mid_C < lastValue ? lastValue - rsi[i].Candle.Mid_C : 0.0;

            lastValue = rsi[i].Candle.Mid_C;
        }

        var gains_rma = gains.RelativeMovingAverage(settings.ShortWindow).ToArray();

        var losses_rma = losses.RelativeMovingAverage(settings.ShortWindow).ToArray();

        for (var i = 0; i < rsi.Length; i++)
        {
            rsi[i].AverageGain = gains_rma[i];

            rsi[i].AverageLoss = losses_rma[i];

            var rs = rsi[i].AverageGain / rsi[i].AverageLoss;

            rsi[i].RSI = 100.0 - 100.0 / (1.0 + rs);
        }

        return rsi;
    }
}