namespace Trading.Bot.Models.DataTransferObjects;

public abstract class PriceBase
{
    public string Instrument { get; set; }
    public double Bid { get; set; }
    public double Ask { get; set; }
}