namespace Trading.Bot.Models.Indicators;

public class MacdEmaResult : Indicator
{
    public double MacdDelta { get; set; }
    public double MacdDeltaPrev { get; set; }
    public int Direction { get; set; }
    public double Ema { get; set; }
}