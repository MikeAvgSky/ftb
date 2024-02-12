namespace Trading.Bot.Models.Indicators;

public class MacdResult : Indicator
{
    public double Macd { get; set; }
    public double SignalLine { get; set; }
    public double Histogram { get; set; }
}
