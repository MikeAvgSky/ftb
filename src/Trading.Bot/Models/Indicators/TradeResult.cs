namespace Trading.Bot.Models.Indicators;

public class TradeResult
{
    public bool Running { get; set; }
    public int StartIndex { get; set; }
    public double StartPrice { get; set; }
    public double TriggerPrice { get; set; }
    public Signal Signal { get; set; }
    public double TakeProfit { get; set; }
    public double StopLoss { get; set; }
    public double Result { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}