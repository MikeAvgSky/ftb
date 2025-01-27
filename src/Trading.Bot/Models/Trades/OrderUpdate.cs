namespace Trading.Bot.Models.Trades;

public class OrderUpdate
{
    public TakeProfit TakeProfit { get; set; }
    public StopLoss StopLoss { get; set; }
    public TrailingStopLoss TrailingStopLoss { get; set; }

    public OrderUpdate(int displayPrecision = 0, double stopLoss = 0, double takeProfit = 0, double trailingStop = 0, string timeInForce = "GTC")
    {
        StopLoss = stopLoss == 0
            ? null
            : new StopLoss
            {
                TimeInForce = timeInForce,
                Price = displayPrecision == 0
                ? stopLoss
                : Math.Round(stopLoss, displayPrecision)
            };
        TakeProfit = takeProfit == 0
            ? null
            : new TakeProfit
            {
                TimeInForce = timeInForce,
                Price = displayPrecision == 0
                ? takeProfit
                : Math.Round(takeProfit, displayPrecision)
            };
        TrailingStopLoss = trailingStop == 0
            ? null
            : new TrailingStopLoss
            {
                TimeInForce = timeInForce,
                Distance = displayPrecision == 0
                    ? trailingStop
                    : Math.Round(trailingStop, displayPrecision)
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

public class TrailingStopLoss
{
    public string TimeInForce { get; set; }
    public double Distance { get; set; }
}