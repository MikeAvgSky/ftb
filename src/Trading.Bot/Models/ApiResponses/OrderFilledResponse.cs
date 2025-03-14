namespace Trading.Bot.Models.ApiResponses;

public class OrderFilledResponse
{
    public decimal AccountBalance { get; set; }
    public string AccountID { get; set; }
    public string BatchID { get; set; }
    public string RequestID { get; set; }
    public decimal Commission { get; set; }
    public decimal Financing { get; set; }
    public decimal FullVWAP { get; set; }
    public FullPrice FullPrice { get; set; }
    public decimal GuaranteedExecutionFee { get; set; }
    public decimal QuoteGuaranteedExecutionFee { get; set; }
    public decimal HalfSpreadCost { get; set; }
    public string Id { get; set; }
    public string Instrument { get; set; }
    public string OrderID { get; set; }
    public decimal Pl { get; set; }
    public decimal QuotePl { get; set; }
    public string Reason { get; set; }
    public DateTime Time { get; set; }
    public TradeOpened TradeOpened { get; set; } = new();
    public TradeClosed[] TradesClosed { get; set; }
    public string Type { get; set; }
    public decimal Units { get; set; }
    public int UserID { get; set; }
}

public class FullPrice
{
    public PriceResponse.Ask[] Asks { get; set; }
    public PriceResponse.Bid[] Bids { get; set; }
    public decimal CloseoutAsk { get; set; }
    public decimal CloseoutBid { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TradeOpened
{
    public string TradeID { get; set; }
    public decimal Units { get; set; }
    public decimal Price { get; set; }
}

public class TradeClosed
{
    public string TradeID { get; set; }
    public decimal Units { get; set; }
    public decimal Price { get; set; }
}