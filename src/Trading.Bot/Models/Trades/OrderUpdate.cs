namespace Trading.Bot.Models.Trades;

public class OrderUpdate
{
    public TakeProfit TakeProfit { get; set; }
    public StopLoss StopLoss { get; set; }

    public OrderUpdate(double stopLoss = 0, double takeProfit = 0, string timeInForce = "GTC")
    {
        StopLoss = stopLoss == 0
            ? null
            : new StopLoss
            {
                TimeInForce = timeInForce,
                Price = stopLoss
            };
        TakeProfit = takeProfit == 0
            ? null
            : new TakeProfit
            {
                TimeInForce = timeInForce,
                Price = takeProfit
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