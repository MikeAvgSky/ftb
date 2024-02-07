namespace Trading.Bot.Models.Indicators;

public class KeltnerChannels : Indicator
{
    public double EMA { get; set; }
    public double KeltnerTop { get; set; }
    public double KeltnerBottom { get; set; }

    private KeltnerChannels(Candle candle)
    {
        Candle = candle;
    }

    public KeltnerChannels() { }

    public static IEnumerable<KeltnerChannels> ProcessCandles(IReadOnlyCollection<Candle> candles, TradeSettings settings)
    {
        var keltner = candles.Select(c => new KeltnerChannels(c)).ToArray();

        var ema = candles.Select(c => c.Mid_C).ExponentialMovingAverage(settings.LongWindow).ToArray();

        var atr = AverageTrueRange.ProcessCandles(candles, settings).ToArray();

        for (var i = 0; i < keltner.Length; i++)
        {
            keltner[i].EMA = ema[i];
            keltner[i].KeltnerTop = atr[i].ATR * 2 + ema[i];
            keltner[i].KeltnerBottom = keltner[i].EMA - atr[i].ATR * 2;
        }

        return keltner;
    }
}