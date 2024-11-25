namespace Trading.Bot.Models.DataTransferObjects;

public class LivePrice : PriceBase
{
    public DateTime Time { get; set; }

    public LivePrice(PriceResponse price)
    {
        Instrument = price.Instrument;
        Price = (price.Bids[0].Price + price.Asks[0].Price) / 2;
        Time = price.Time;
    }
}