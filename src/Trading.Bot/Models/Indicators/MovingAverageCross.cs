namespace Trading.Bot.Models.Indicators;

public class MovingAverageCross : Indicator
{
    public double MaShort { get; set; }
    public double MaLong { get; set; }
    public double Delta { get; set; }
    public double DeltaPrev { get; set; }

    public MovingAverageCross(Candle candle)
    {
        Candle = candle;
    }

    public MovingAverageCross() { }

    public static IEnumerable<MovingAverageCross> ProcessCandles(IReadOnlyCollection<Candle> candles, TradeSettings settings)
    {
        var mac = candles.Select(c => new MovingAverageCross(c)).ToArray();

        var typicalPrice = candles.Select(c => (c.Mid_C + c.Mid_H + c.Mid_L) / 3).ToArray();

        var maShort = typicalPrice.SimpleMovingAverage(settings.ShortWindow).ToArray();

        var maLong = typicalPrice.SimpleMovingAverage(settings.LongWindow).ToArray();

        for (var i = 0; i < mac.Length; i++)
        {
            mac[i].MaShort = maShort[i];

            mac[i].MaLong = maLong[i];

            mac[i].Delta = maShort[i] - maLong[i];

            mac[i].DeltaPrev = i > 0 ? mac[i - 1].Delta : 0;

            mac[i].Spread = ApplySpread(mac[i]);

            mac[i].Signal = mac[i].Delta switch
            {
                >= 0 when mac[i].DeltaPrev < 0 => Signal.Buy,
                < 0 when mac[i].DeltaPrev >= 0 => Signal.Sell,
                _ => Signal.None
            };

            var diff = i < mac.Length - 1
                ? mac[i + 1].Candle.Mid_C - mac[i].Candle.Mid_C
                : mac[i].Candle.Mid_C;

            mac[i].Gain = Math.Abs(diff * (int)mac[i].Signal);

            mac[i].TakeProfit = ApplyTakeProfit(mac[i]);

            mac[i].StopLoss = ApplyStopLoss(mac[i], settings);

            mac[i].Loss = Math.Abs(mac[i].Candle.Mid_C - mac[i].StopLoss);
        }

        return mac;
    }
}