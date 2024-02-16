namespace Trading.Bot.Models.Trades;

public class OrderCancelTransaction
{
    public string AccountID { get; set; }
    public string BatchID { get; set; }
    public string RequestID { get; set; }
    public string Id { get; set; }
    public string Reason { get; set; }
    public DateTime Time { get; set; }
    public string Type { get; set; }
    public string OrderID { get; set; }
    public int UserID { get; set; }
}