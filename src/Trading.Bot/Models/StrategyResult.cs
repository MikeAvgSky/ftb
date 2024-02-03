namespace Trading.Bot.Models;

public class StrategyResult
{
    public string Instrument { get; set; }
    public string Granularity { get; set; }
    public string Strategy { get; set; }
    public int TradeCount { get; set; }
    public double TotalGain { get; set; }
    public double MeanGain { get; set; }
    public double MinGain { get; set; }
    public double MaxGain { get; set; }
}