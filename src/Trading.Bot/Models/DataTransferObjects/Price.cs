namespace Trading.Bot.Models.DataTransferObjects;

public class Price
{
    public string Instrument { get; set; }
    public double Bid { get; set; }
    public double Ask { get; set; }
    public double HomeConversion { get; set; }

    public Price(PriceResponse price, HomeConversionResponse conversion)
    {
        Instrument = price.Instrument;
        Bid = price.Bids[0].Price;
        Ask = price.Asks[0].Price;
        HomeConversion = conversion.PositionValue;
    }
}