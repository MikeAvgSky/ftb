namespace Trading.Bot.Configuration;

public class TradeConfiguration
{
    public bool StopRollover { get; set; }
    public int TradeRisk { get; set; }
    public TradeSettings[] TradeSettings { get; set; }
}