namespace Trading.Bot.Models.Indicators;

public class AverageTrueRange : Indicator
{
    public double TrueRangeA { get; set; }
    public double TrueRangeB { get; set; }
    public double TrueRangeC { get; set; }
    public double MaxTrueRange { get; set; }
    public double ATR { get; set; }

    private AverageTrueRange(Candle candle)
    {
        Candle = candle;
    }

    public AverageTrueRange() { }

    public static IEnumerable<AverageTrueRange> ProcessCandles(IReadOnlyCollection<Candle> candles, TradeSettings settings)
    {
        var atr = candles.Select(c => new AverageTrueRange(c)).ToArray();

        for (var i = 0; i < atr.Length; i++)
        {
            var prevMidC = i == 0 ? atr[i].Candle.Mid_C : atr[i - 1].Candle.Mid_C;

            atr[i].TrueRangeA = atr[i].Candle.Mid_H - atr[i].Candle.Mid_L;

            atr[i].TrueRangeB = Math.Abs(atr[i].Candle.Mid_H - prevMidC);

            atr[i].TrueRangeC = Math.Abs(prevMidC - atr[i].Candle.Mid_L);

            var trueRanges = new[] { atr[i].TrueRangeA, atr[i].TrueRangeB, atr[i].TrueRangeC };

            atr[i].MaxTrueRange = trueRanges.Max();
        }

        var tr = atr.Select(x => x.MaxTrueRange).SimpleMovingAverage(settings.LongWindow).ToArray();

        for (var i = 0; i < atr.Length; i++)
        {
            atr[i].ATR = tr[i];
        }

        return atr;
    }
}