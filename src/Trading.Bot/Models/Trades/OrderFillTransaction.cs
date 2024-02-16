namespace Trading.Bot.Models.Trades;

public class OrderFillTransaction
{ 
    public string AccountBalance { get; set; }
    public string AccountID { get; set; }
    public string BatchID { get; set; }
    public string Financing { get; set; }
    public string Id { get; set; }
    public string Instrument { get; set; }
    public string OrderID { get; set; }
    public string Pl { get; set; }
    public string Price { get; set; }
    public string Reason { get; set; }
    public DateTime Time { get; set; }
    public TradeOpened tradeOpened { get; set; }
    public string Type { get; set; }
    public double Units { get; set; }
    public int UserID { get; set; }
}

public class TradeOpened
{
    public string TradeID { get; set; }
    public double Units { get; set; }
}