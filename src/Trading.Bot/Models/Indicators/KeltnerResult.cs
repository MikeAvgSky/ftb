namespace Trading.Bot.Models.Indicators;

public class KeltnerResult : Indicator
{
    public double Ema { get; set; }
    public double KeltnerTop { get; set; }
    public double KeltnerBottom { get; set; }
}