namespace Trading.Bot.Extensions;

public static class ApiServiceMapperExtensions
{
    public static Candle[] MapToCandles(this CandleData[] candles)
    {
        var length = candles.Count(c => c.Complete);

        var result = new Candle[length];

        for (var i = 0; i < length; i++)
        {
            if (!candles[i].Complete) continue;

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

    public static Price[] MapToPrices(this PricingResponse pricingResponse)
    {
        var length = pricingResponse.Prices.Length;

        var result = new Price[length];

        for (var i = 0; i < length; i++)
        {
            var baseInstrument = pricingResponse.Prices[i].Instrument.Split('_')[1];

            result[i] = new Price(pricingResponse.Prices[i],
                pricingResponse.HomeConversions.First(c => c.Currency == baseInstrument));
        }

        return result;
    }
}