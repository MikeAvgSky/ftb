namespace Trading.Bot.Models.DataTransferObjects;

public class Candle
{
    public DateTime Time { get; set; }
    public int Volume { get; set; }
    public double Mid_O { get; set; }
    public double Mid_H { get; set; }
    public double Mid_L { get; set; }
    public double Mid_C { get; set; }
    public double Bid_O { get; set; }
    public double Bid_H { get; set; }
    public double Bid_L { get; set; }
    public double Bid_C { get; set; }
    public double Ask_O { get; set; }
    public double Ask_H { get; set; }
    public double Ask_L { get; set; }
    public double Ask_C { get; set; }
    public double Spread { get; set; }
    public double BodySize { get; set; }
    public int Direction { get; set; }
    public double FullRange { get; set; }
    public double BodyPercentage { get; set; }
    public double BodyLower { get; set; }
    public double BodyUpper { get; set; }
    public double BodyBottomPercentage { get; set; }
    public double BodyTopPercentage { get; set; }
    public double MidPoint { get; set; }

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
        BodyPercentage = (BodySize / FullRange * 100).NaN2Zero();
        BodyLower = new[] { Mid_C, Mid_O }.Min();
        BodyUpper = new[] { Mid_C, Mid_O }.Max();
        BodyBottomPercentage = ((BodyLower - Mid_L) / FullRange * 100).NaN2Zero();
        BodyTopPercentage = ((Mid_H - BodyUpper) / FullRange * 100).NaN2Zero();
        MidPoint = (FullRange / 2 + Mid_L).NaN2Zero();
    }

    public Candle() { }
}