namespace Trading.Bot.Models.Trades;

public class OrderUpdate
{
    public TakeProfit TakeProfit { get; set; }
    public StopLoss StopLoss { get; set; }

    public OrderUpdate(int displayPrecision, double stopLoss = 0, double takeProfit = 0, string timeInForce = "GTC")
    {
        StopLoss = stopLoss == 0
            ? null
            : new StopLoss
            {
                TimeInForce = timeInForce,
                Price = Math.Round(stopLoss, displayPrecision)
            };
        TakeProfit = takeProfit == 0
            ? null
            : new TakeProfit
            {
                TimeInForce = timeInForce,
                Price = Math.Round(takeProfit, displayPrecision)
            };
    }
}

public class StopLoss
{
    public string TimeInForce { get; set; }
    public double Price { get; set; }
}

public class TakeProfit
{
    public string TimeInForce { get; set; }
    public double Price { get; set; }
}