namespace Trading.Bot.Models.Indicators;

public abstract class IndicatorBase
{
    public Candle Candle { get; set; }
    public Signal Signal { get; set; }
    public decimal Gain { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Loss { get; set; }
}