namespace Trading.Bot.Models.Trades;

public class TrailingStop
{
    public string TradeId { get; set; }
    public double StopLoss { get; set; }
    public int DisplayPrecision { get; set; }

    public TrailingStop(string tradeId, double stopLoss, int displayPrecision)
    {
        TradeId = tradeId;
        StopLoss = stopLoss;
        DisplayPrecision = displayPrecision;
    }
}