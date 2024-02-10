namespace Trading.Bot.Models.Indicators;

public class RsiResult : Indicator
{
    public double AverageGain { get; set; }
    public double AverageLoss { get; set; }
    public double Rsi { get; set; }
}