namespace Trading.Bot.Models.Indicators;

public class Indicator
{
    public Candle Candle { get; set; }
    public Signal Signal { get; set; }
    public double Gain { get; set; }
    public double TakeProfit { get; set; }
    public double StopLoss { get; set; }
    public double Loss { get; set; }
}