namespace Trading.Bot.Models.Indicators;

public class BollingerBands : Indicator
{
    public double BollingerAverage { get; set; }
    public double BollingerTop { get; set; }
    public double BollingerBottom { get; set; }

    private BollingerBands(Candle candle)
    {
        Candle = candle;
    }

    public BollingerBands() { }

    public static IEnumerable<BollingerBands> ProcessCandles(IReadOnlyCollection<Candle> candles, TradeSettings settings)
    {
        var bb = candles.Select(c => new BollingerBands(c)).ToArray();

        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var standardDeviation = typicalPrice
            .MovingStandardDeviation(settings.LongWindow, settings.StandardDeviation).ToArray();

        var bollingerBandsAverage = typicalPrice.SimpleMovingAverage(settings.LongWindow).ToArray();

        for (var i = 0; i < bb.Length; i++)
        {
            bb[i].BollingerAverage = bollingerBandsAverage[i];

            bb[i].BollingerTop = bb[i].BollingerAverage + standardDeviation[i] * settings.StandardDeviation;

            bb[i].BollingerBottom = bb[i].BollingerAverage - standardDeviation[i] * settings.StandardDeviation;

            bb[i].Spread = ApplySpread(bb[i]);

            bb[i].Signal = bb[i].Candle switch
            {
                var candle when candle.Mid_C < bb[i].BollingerBottom &&
                                candle.Mid_O > bb[i].BollingerBottom => Signal.Buy,
                var candle when candle.Mid_C > bb[i].BollingerTop &&
                                candle.Mid_O < bb[i].BollingerTop => Signal.Sell,
                _ => Signal.None
            };

            bb[i].Gain = Math.Abs(bb[i].Candle.Mid_C - bb[i].BollingerAverage);

            bb[i].TakeProfit = ApplyTakeProfit(bb[i]);

            bb[i].StopLoss = ApplyStopLoss(bb[i], settings);

            bb[i].Loss = Math.Abs(bb[i].Candle.Mid_C - bb[i].StopLoss);
        }

        return bb;
    }
}