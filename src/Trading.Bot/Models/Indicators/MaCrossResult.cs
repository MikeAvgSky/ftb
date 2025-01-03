namespace Trading.Bot.Models.Indicators;

public class MaCrossResult : IndicatorResult
{
    public double MaShort { get; set; }
    public double MaLong { get; set; }
    public double Delta { get; set; }
    public double DeltaPrev { get; set; }
}