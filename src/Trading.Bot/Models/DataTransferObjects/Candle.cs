namespace Trading.Bot.Models.DataTransferObjects;

public class Candle
{
    public DateTime Time { get; set; }
    public int Volume { get; set; }
    public decimal Mid_O { get; set; }
    public decimal Mid_H { get; set; }
    public decimal Mid_L { get; set; }
    public decimal Mid_C { get; set; }
    public decimal Bid_O { get; set; }
    public decimal Bid_H { get; set; }
    public decimal Bid_L { get; set; }
    public decimal Bid_C { get; set; }
    public decimal Ask_O { get; set; }
    public decimal Ask_H { get; set; }
    public decimal Ask_L { get; set; }
    public decimal Ask_C { get; set; }
    public decimal Spread { get; set; }
    public decimal BodySize { get; set; }
    public int Direction { get; set; }
    public decimal FullRange { get; set; }
    public decimal BodyPercentage { get; set; }
    public decimal BodyLower { get; set; }
    public decimal BodyUpper { get; set; }
    public decimal BodyBottomPercentage { get; set; }
    public decimal BodyTopPercentage { get; set; }
    public decimal MidPoint { get; set; }

    public Candle(CandleData data)
    {
        Time = data.Time;
        Volume = data.Volume;
        Mid_O = data.Mid.O;
        Mid_H = data.Mid.H;
        Mid_L = data.Mid.L;
        Mid_C = data.Mid.C;
        Bid_O = data.Bid.O;
        Bid_H = data.Bid.H;
        Bid_L = data.Bid.L;
        Bid_C = data.Bid.C;
        Ask_O = data.Ask.O;
        Ask_H = data.Ask.H;
        Ask_L = data.Ask.L;
        Ask_C = data.Ask.C;
        Spread = Ask_C - Bid_C;
        BodySize = Math.Abs(Mid_C - Mid_O);
        Direction = Mid_C - Mid_O >= 0 ? 1 : -1;
        FullRange = Mid_H - Mid_L;
        BodyPercentage = BodySize / FullRange * 100;
        BodyLower = new[] { Mid_C, Mid_O }.Min();
        BodyUpper = new[] { Mid_C, Mid_O }.Max();
        BodyBottomPercentage = (BodyLower - Mid_L) / FullRange * 100;
        BodyTopPercentage = (Mid_H - BodyUpper) / FullRange * 100;
        MidPoint = FullRange / 2 + Mid_L;
    }

    public Candle() { }
}