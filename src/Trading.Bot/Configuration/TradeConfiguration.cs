namespace Trading.Bot.Configuration;

public class TradeConfiguration
{
    public bool RunBot { get; set; }
    public bool StopRollover { get; set; }
    public bool CheckHigherTimeFrame { get; set; }
    public int TradeRisk { get; set; }
    public TradeSettings[] TradeSettings { get; set; }
}