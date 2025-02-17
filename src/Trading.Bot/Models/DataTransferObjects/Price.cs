namespace Trading.Bot.Models.DataTransferObjects;

public class Price : PriceBase
{
    public decimal HomeConversion { get; set; }

    public Price(PriceResponse price, HomeConversionResponse conversion)
    {
        Instrument = price.Instrument;
        Price = (price.Bids[0].Price + price.Asks[0].Price) / 2;
        HomeConversion = conversion.PositionValue;
    }
}