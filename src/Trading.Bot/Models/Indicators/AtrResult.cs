namespace Trading.Bot.Models.Indicators;

public class AtrResult : Indicator
{
    public double TrA { get; set; }
    public double TrB { get; set; }
    public double TrC { get; set; }
    public double MaxTr { get; set; }
    public double Atr { get; set; }
}