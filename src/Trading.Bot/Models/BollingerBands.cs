namespace Trading.Bot.Models;

public class BollingerBands : Indicator
{
    public double BollingerAverage { get; set; }
    public double BollingerTop { get; set; }
    public double BollingerBottom { get; set; }

    public static IEnumerable<BollingerBands> ProcessCandles(IReadOnlyCollection<Candle> candles, TradeSettings tradeSettings)
    {
        var bollingerBands = candles.Select(c => new BollingerBands(c)).ToList();

        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToList();

        var standardDeviation = typicalPrice
            .MovingStandardDeviation(tradeSettings.MovingAverageWindow, tradeSettings.StandardDeviation).ToList();

        var bollingerBandsAverage = typicalPrice.MovingAverage(tradeSettings.MovingAverageWindow).ToList();

        for (var i = 0; i < bollingerBands.Count; i++)
        {
            bollingerBands[i].BollingerAverage = bollingerBandsAverage[i];

            bollingerBands[i].BollingerTop = bollingerBands[i].BollingerAverage + standardDeviation[i] * tradeSettings.StandardDeviation;

            bollingerBands[i].BollingerBottom = bollingerBands[i].BollingerAverage - standardDeviation[i] * tradeSettings.StandardDeviation;

            bollingerBands[i].Spread = ApplySpread(bollingerBands[i]);

            bollingerBands[i].Signal = bollingerBands[i].Candle switch
            {
                var candle when candle.Mid_C < bollingerBands[i].BollingerBottom && 
                                candle.Mid_O > bollingerBands[i].BollingerBottom => Signal.Buy,
                var candle when candle.Mid_C > bollingerBands[i].BollingerTop && 
                                candle.Mid_O < bollingerBands[i].BollingerTop => Signal.Sell,
                _ => Signal.None
            };

            bollingerBands[i].Gain = Math.Abs(bollingerBands[i].Candle.Mid_C - bollingerBands[i].BollingerAverage);

            bollingerBands[i].TakeProfit = ApplyTakeProfit(bollingerBands[i]);

            bollingerBands[i].StopLoss = ApplyStopLoss(bollingerBands[i], tradeSettings);

            bollingerBands[i].Loss = Math.Abs(bollingerBands[i].Candle.Mid_C - bollingerBands[i].StopLoss);
        }

        return bollingerBands;
    }

    private BollingerBands(Candle candle)
    {
        Candle = candle;
    }

    public BollingerBands() { }
}