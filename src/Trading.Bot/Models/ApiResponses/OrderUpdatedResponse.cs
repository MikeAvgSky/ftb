namespace Trading.Bot.Models.ApiResponses;

public class OrderUpdatedResponse
{
    public string LastTransactionID { get; set; }
    public string[] RelatedTransactionIDs { get; set; }
    public StopLossOrderTransaction StopLossOrderTransaction { get; set; }
    public TakeProfitOrderTransaction TakeProfitOrderTransaction { get; set; }
}

public class StopLossOrderTransaction
{
    public string AccountID { get; set; }
    public string BatchID { get; set; }
    public string ClientTradeID { get; set; }
    public string Id { get; set; }
    public string Price { get; set; }
    public string Reason { get; set; }
    public DateTime Time { get; set; }
    public string TimeInForce { get; set; }
    public string TradeID { get; set; }
    public string TriggerCondition { get; set; }
    public string Type { get; set; }
    public int UserID { get; set; }
}

public class TakeProfitOrderTransaction
{
    public string AccountID { get; set; }
    public string BatchID { get; set; }
    public string ClientTradeID { get; set; }
    public string Id { get; set; }
    public string Price { get; set; }
    public string Reason { get; set; }
    public DateTime Time { get; set; }
    public string TimeInForce { get; set; }
    public string TradeID { get; set; }
    public string TriggerCondition { get; set; }
    public string Type { get; set; }
    public int UserID { get; set; }
}