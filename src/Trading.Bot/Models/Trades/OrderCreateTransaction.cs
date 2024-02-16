namespace Trading.Bot.Models.Trades;

public class OrderCreateTransaction
{
    public string AccountID { get; set; }
    public string BatchID { get; set; }
    public string RequestID { get; set; }
    public string Id { get; set; }
    public string Instrument { get; set; }
    public string PositionFill { get; set; }
    public string Reason { get; set; }
    public DateTime Time { get; set; }
    public string TimeInForce { get; set; }
    public string Type { get; set; }
    public double Units { get; set; }
    public int UserID { get; set; }
}