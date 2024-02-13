namespace Trading.Bot.Models.Indicators;

public class RsiResult : IndicatorBase
{
    public double AverageGain { get; set; }
    public double AverageLoss { get; set; }
    public double Rsi { get; set; }
}