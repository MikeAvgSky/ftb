namespace Trading.Bot.Configuration;

public class TradeSettings
{
    public string Instrument { get; set; }
    public int ShortWindow { get; set; }
    public int LongWindow { get; set; }
    public int RsiWindow { get; set; }
    public int Signal { get; set; }
    public int StandardDeviation { get; set; }
    public double MaxSpread { get; set; }
    public double MinSpread { get; set; }
    public int RiskReward { get; set; }
}