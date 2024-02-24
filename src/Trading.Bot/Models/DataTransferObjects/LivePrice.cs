namespace Trading.Bot.Models.DataTransferObjects;

public class LivePrice : PriceBase
{
    public int SellVolume { get; set; }
    public int BuyVolume { get; set; }
    public double Mid { get; set; }
    public DateTime Time { get; set; }

    public LivePrice(PriceResponse price, int displayPrecision)
    {
        Instrument = price.Instrument;
        Bid = price.Bids[0].Price;
        Ask = price.Asks[0].Price;
        SellVolume = price.Bids.Length;
        BuyVolume = price.Asks.Length;
        Mid = Math.Round((Bid + Ask) / 2, displayPrecision);
        Time = price.Time;
    }
}