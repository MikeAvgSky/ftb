namespace Trading.Bot.Models.ApiResponses;

public class OrderFilledResponse
{
    public double AccountBalance { get; set; }
    public string AccountID { get; set; }
    public string BatchID { get; set; }
    public string RequestID { get; set; }
    public double Commission { get; set; }
    public double Financing { get; set; }
    public double FullVWAP { get; set; }
    public FullPrice FullPrice { get; set; }
    public double GuaranteedExecutionFee { get; set; }
    public double QuoteGuaranteedExecutionFee { get; set; }
    public double HalfSpreadCost { get; set; }
    public string Id { get; set; }
    public string Instrument { get; set; }
    public string OrderID { get; set; }
    public double Pl { get; set; }
    public double QuotePl { get; set; }
    public string Reason { get; set; }
    public DateTime Time { get; set; }
    public TradeOpened TradeOpened { get; set; }
    public TradeClosed[] TradesClosed { get; set; }
    public string Type { get; set; }
    public double Units { get; set; }
    public int UserID { get; set; }
}

public class FullPrice
{
    public PriceResponse.Ask[] Asks { get; set; }
    public PriceResponse.Bid[] Bids { get; set; }
    public double CloseoutAsk { get; set; }
    public double CloseoutBid { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TradeOpened
{
    public string TradeID { get; set; }
    public double Units { get; set; }
    public double Price { get; set; }
}

public class TradeClosed
{
    public string TradeID { get; set; }
    public double Units { get; set; }
    public double Price { get; set; }
}