namespace Trading.Bot.Models.DataTransferObjects;

public class Price : PriceBase
{
    public double HomeConversion { get; set; }

    public Price(PriceResponse price, HomeConversionResponse conversion)
    {
        Instrument = price.Instrument;
        Bid = price.Bids[0].Price;
        Ask = price.Asks[0].Price;
        HomeConversion = conversion.PositionValue;
    }
}