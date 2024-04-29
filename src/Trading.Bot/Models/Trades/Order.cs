namespace Trading.Bot.Models.Trades;

public class Order
{
    public string Type { get; set; }
    public string Instrument { get; set; }
    public double Units { get; set; }
    public string TimeInForce { get; set; }
    public string PositionFill { get; set; }
    public StopLossOnFill StopLossOnFill { get; set; }
    public TakeProfitOnFill TakeProfitOnFill { get; set; }
    public TrailingStopLossOnFill TrailingStopLossOnFill { get; set; }

    public Order(Instrument instrument, double units, Signal signal, double stopLoss = 0, double takeProfit = 0,
        double trailingStop = 0, string type = "MARKET", string timeInForce = "FOK", string positionFill = "DEFAULT")
    {
        Type = type;
        Instrument = instrument.Name;
        if (signal == Signal.Sell)
            units *= -1;
        Units = Math.Round(units, instrument.TradeUnitsPrecision);
        TimeInForce = timeInForce;
        PositionFill = positionFill;
        StopLossOnFill = stopLoss == 0
            ? null
            : new StopLossOnFill
            {
                Price = Math.Round(stopLoss, instrument.DisplayPrecision)
            };
        TakeProfitOnFill = takeProfit == 0
            ? null
            : new TakeProfitOnFill
            {
                Price = Math.Round(takeProfit, instrument.DisplayPrecision)
            };
        TrailingStopLossOnFill = trailingStop == 0
            ? null
            : new TrailingStopLossOnFill
            {
                Distance = Math.Round(trailingStop, instrument.DisplayPrecision)
            };
    }
}

public class StopLossOnFill
{
    public double Price { get; set; }
}

public class TakeProfitOnFill
{
    public double Price { get; set; }
}

public class TrailingStopLossOnFill
{
    public double Distance { get; set; }
}