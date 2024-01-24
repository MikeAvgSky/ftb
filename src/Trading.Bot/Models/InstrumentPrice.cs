namespace Trading.Bot.Models;

public class InstrumentPrice
{
    public string Type { get; set; }
    public DateTime Time { get; set; }
    public Bid[] Bids { get; set; }
    public Ask[] Asks { get; set; }
    public double CloseoutBid { get; set; }
    public double CloseoutAsk { get; set; }
    public string Status { get; set; }
    public bool Tradeable { get; set; }
    public HomeConversionFactors QuoteHomeConversionFactors { get; set; }
    public string Instrument { get; set; }

    public class HomeConversionFactors
    {
        public double PositiveUnits { get; set; }
        public double NegativeUnits { get; set; }
    }

    public class Bid
    {
        public double Price { get; set; }
        public int Liquidity { get; set; }
    }

    public class Ask
    {
        public double Price { get; set; }
        public int Liquidity { get; set; }
    }
}