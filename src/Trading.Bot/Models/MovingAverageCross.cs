namespace Trading.Bot.Models;

public class MovingAverageCross : Strategy
{
    public Candle Candle { get; set; }
    public double MaShort { get; set; }
    public double MaLong { get; set; }
    public double Delta { get; set; }
    public double DeltaPrev { get; set; }

    public MovingAverageCross(Candle candle)
    {
        Candle = candle;
    }

    public MovingAverageCross() {}
}