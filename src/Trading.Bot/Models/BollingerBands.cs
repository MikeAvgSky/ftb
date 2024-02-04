namespace Trading.Bot.Models;

public class BollingerBands : Strategy
{
    public Candle Candle { get; set; }
    public double BollingerAverage { get; set; }
    public double BollingerTop { get; set; }
    public double BollingerBottom { get; set; }

    public BollingerBands(Candle candle)
    {
        Candle = candle;
    }

    public BollingerBands() { }
}