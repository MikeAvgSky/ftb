namespace Trading.Bot.Configuration;

public class TradeSettings
{
    public string Instrument { get; set; }
    public string MainGranularity { get; set; }
    public string[] OtherGranularities { get; set; }
    public TimeSpan CandleSpan { get; set; }
    public int[] Integers { get; set; }
    public double[] Doubles { get; set; }
    public decimal MaxSpread { get; set; }
    public decimal MinGain { get; set; }
    public decimal RiskReward { get; set; }
    public bool TrailingStop { get; set; }
}