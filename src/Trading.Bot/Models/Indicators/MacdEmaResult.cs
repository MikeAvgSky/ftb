namespace Trading.Bot.Models.Indicators;

public class MacdEmaResult : IndicatorBase
{
    public double MacdDelta { get; set; }
    public double MacdDeltaPrev { get; set; }
    public int Direction { get; set; }
    public double Ema { get; set; }
}