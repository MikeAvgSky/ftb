namespace Trading.Bot.Models.Indicators;

public class BollingerBandsResult : Indicator
{
    public double BollingerSma { get; set; }
    public double BollingerTop { get; set; }
    public double BollingerBottom { get; set; }
}