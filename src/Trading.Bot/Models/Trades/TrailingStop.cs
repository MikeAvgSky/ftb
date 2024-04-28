namespace Trading.Bot.Models.Trades;

public class TrailingStop
{
    public string TradeId { get; set; }
    public double StopLossTarget { get; set; }
    public double RiskReward { get; set; }
    public int DisplayPrecision { get; set; }
}