namespace Trading.Bot.Configuration;

public class TradeSettings
{
    public string Instrument { get; set; }
    public string MainGranularity { get; set; }
    public string[] OtherGranularities { get; set; }
    public TimeSpan CandleSpan { get; set; }
    public int[] Integers { get; set; }
    public double[] Doubles { get; set; }
    public double MaxSpread { get; set; }
    public double MinGain { get; set; }
    public double RiskReward { get; set; }
    public bool TrailingStop { get; set; }
}