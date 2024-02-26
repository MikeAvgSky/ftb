namespace Trading.Bot.Models.DataTransferObjects;

public class LivePrice : PriceBase
{
    public DateTime Time { get; set; }

    public LivePrice(PriceResponse price)
    {
        Instrument = price.Instrument;
        Bid = price.Bids[0].Price;
        Ask = price.Asks[0].Price;
        Time = price.Time;
    }
}