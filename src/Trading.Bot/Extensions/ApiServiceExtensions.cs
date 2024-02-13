namespace Trading.Bot.Extensions;

public static class ApiServiceExtensions
{
    public static Candle[] MapToCandles(this CandleData[] candles)
    {
        var length = candles.Length;

        var result = new Candle[length];

        for (var i = 0; i < length; i++)
        {
            result[i] = new Candle(candles[i]);
        }

        return result;
    }

    public static Instrument[] MapToInstruments(this List<InstrumentResponse> instruments)
    {
        var length = instruments.Count;

        var result = new Instrument[length];

        for (var i = 0; i < length; i++)
        {
            result[i] = new Instrument(instruments[i]);
        }

        return result;
    }
}