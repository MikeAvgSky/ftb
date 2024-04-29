namespace Trading.Bot.Models.Trades;

public class TrailingStop
{
    public string TradeId { get; set; }
    public Signal Signal { get; set; }
    public double StopLossTarget { get; set; }
    public int DisplayPrecision { get; set; }
}