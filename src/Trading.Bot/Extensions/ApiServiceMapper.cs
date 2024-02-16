namespace Trading.Bot.Extensions;

public static class ApiServiceMapper
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

    public static Instrument[] MapToInstruments(this InstrumentResponse[] instruments)
    {
        var length = instruments.Length;

        var result = new Instrument[length];

        for (var i = 0; i < length; i++)
        {
            result[i] = new Instrument(instruments[i]);
        }

        return result;
    }
}