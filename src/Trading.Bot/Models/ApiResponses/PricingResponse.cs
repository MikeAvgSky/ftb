namespace Trading.Bot.Models.ApiResponses;

public class PricingResponse
{
    public DateTime Time { get; set; }
    public PriceResponse[] Prices { get; set; }
    public HomeConversionResponse[] HomeConversions { get; set; }
}

public class PriceResponse
{
    public string Type { get; set; }
    public DateTime Time { get; set; }
    public Bid[] Bids { get; set; }
    public Ask[] Asks { get; set; }
    public decimal CloseoutBid { get; set; }
    public decimal CloseoutAsk { get; set; }
    public string Status { get; set; }
    public bool Tradeable { get; set; }
    public string Instrument { get; set; }

    public class Bid
    {
        public decimal Price { get; set; }
        public int Liquidity { get; set; }
    }

    public class Ask
    {
        public decimal Price { get; set; }
        public int Liquidity { get; set; }
    }
}

public class HomeConversionResponse
{
    public string Currency { get; set; }
    public decimal AccountGain { get; set; }
    public decimal AccountLoss { get; set; }
    public decimal PositionValue { get; set; }
}
