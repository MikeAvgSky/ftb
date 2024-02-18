namespace Trading.Bot.Configuration;

public class TradeSettings
{
    public string Instrument { get; set; }
    public string Granularity { get; set; }
    public int MovingAverage { get; set; }
    public double StandardDeviation { get; set; }
    public double MaxSpread { get; set; }
    public double MinGain { get; set; }
    public double RiskReward { get; set; }
}