namespace Trading.Bot.Models.Indicators;

public class Indicator
{
    public Candle Candle { get; set; }
    public double Spread { get; set; }
    public Signal Signal { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public double Loss { get; set; }
    public double Gain { get; set; }

    public static double ApplySpread(Indicator indicator)
    {
        return indicator.Candle.Ask_C - indicator.Candle.Bid_C;
    }

    public static double ApplyTakeProfit(Indicator indicator)
    {
        return indicator.Signal switch
        {
            Signal.Buy => indicator.Candle.Mid_C + indicator.Gain,
            Signal.Sell => indicator.Candle.Mid_C - indicator.Gain,
            _ => 0.0
        };
    }

    public static double ApplyStopLoss(Indicator indicator, TradeSettings tradeSettings)
    {
        return indicator.Signal switch
        {
            Signal.Buy => indicator.Candle.Mid_C - indicator.Gain / tradeSettings.RiskReward,
            Signal.Sell => indicator.Candle.Mid_C + indicator.Gain / tradeSettings.RiskReward,
            _ => 0.0
        };
    }
}