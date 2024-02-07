namespace Trading.Bot.Models.Indicators;

public class MACD : Indicator
{
    public double Macd { get; set; }
    public double Macd_Signal { get; set; }
    public double Histogram { get; set; }

    public MACD(Candle candle)
    {
        Candle = candle;
    }

    public MACD() { }

    public static IEnumerable<MACD> ProcessCandles(IReadOnlyCollection<Candle> candles, TradeSettings settings)
    {
        var macd = candles.Select(c => new MACD(c)).ToArray();

        var emaLong = candles.Select(c => c.Mid_C).ExponentialMovingAverage(settings.LongWindow).ToArray();

        var emaShort = candles.Select(c => c.Mid_C).ExponentialMovingAverage(settings.ShortWindow).ToArray();

        for (var i = 0; i < macd.Length; i++)
        {
            macd[i].Macd = emaShort[i] - emaLong[i];
        }

        var signal = macd.Select(m => m.Macd).ExponentialMovingAverage(settings.Signal).ToArray();

        for (var i = 0; i < macd.Length; i++)
        {
            macd[i].Macd_Signal = signal[i];

            macd[i].Histogram = macd[i].Macd - macd[i].Macd_Signal;
        }

        return macd;
    }

}