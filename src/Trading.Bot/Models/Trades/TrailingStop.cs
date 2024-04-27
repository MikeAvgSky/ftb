namespace Trading.Bot.Models.Trades;

public class TrailingStop
{
    public string TradeId { get; set; }
    public double StopLoss { get; set; }

    public TrailingStop(string tradeId, double stopLoss)
    {
        TradeId = tradeId;
        StopLoss = stopLoss;
    }
}