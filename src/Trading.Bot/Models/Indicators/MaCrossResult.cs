namespace Trading.Bot.Models.Indicators;

public class MaCrossResult : IndicatorBase
{
    public double MaShort { get; set; }
    public double MaLong { get; set; }
    public double Delta { get; set; }
    public double DeltaPrev { get; set; }
}