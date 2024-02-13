namespace Trading.Bot.Models.Indicators;

public class MacdResult : IndicatorBase
{
    public double Macd { get; set; }
    public double SignalLine { get; set; }
    public double Histogram { get; set; }
}
