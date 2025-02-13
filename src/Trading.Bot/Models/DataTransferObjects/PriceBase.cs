namespace Trading.Bot.Models.DataTransferObjects;

public abstract class PriceBase
{
    public string Instrument { get; set; }
    public decimal Price { get; set; }
}