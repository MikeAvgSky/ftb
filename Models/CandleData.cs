namespace Trading.Bot.Models;

public class CandleData
{
    public string Instrument { get; set; }
    public string Granularity { get; set; }
    public Candle[] Candles { get; set; }
}

public class Candle
{
    public bool Complete { get; set; }
    public int Volume { get; set; }
    public DateTime Time { get; set; }
    public CandlestickData Bid { get; set; } = new();
    public CandlestickData Mid { get; set; } = new();
    public CandlestickData Ask { get; set; } = new();
}

public class CandlestickData
{
    public double O { get; set; }
    public double H { get; set; }
    public double L { get; set; }
    public double C { get; set; }
}