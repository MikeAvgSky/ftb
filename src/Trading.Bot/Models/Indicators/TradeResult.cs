namespace Trading.Bot.Models.Indicators;

public class TradeResult
{
    public bool Running { get; set; }
    public int StartIndex { get; set; }
    public double StartPrice { get; set; }
    public double TriggerPrice { get; set; }
    public Signal Signal { get; set; }
    public double TakeProfit { get; set; }
    public double StopLoss { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double Result { get; set; }

    public static TradeResult[] SimulateTrade(IndicatorBase[] indicators)
    {
        var length = indicators.Length;

        var openTrades = new List<TradeResult>();

        var closedTrades = new List<TradeResult>();

        for (var i = 0; i < length; i++)
        {
            if (indicators[i].Signal != Signal.None && !openTrades.Any())
            {
                openTrades.Add(new TradeResult
                {
                    Running = true,
                    StartIndex = i,
                    StartPrice = indicators[i].Signal == Signal.Buy
                        ? indicators[i].Candle.Ask_C
                        : indicators[i].Candle.Bid_C,
                    TriggerPrice = indicators[i].Signal == Signal.Buy
                        ? indicators[i].Candle.Ask_C
                        : indicators[i].Candle.Bid_C,
                    Signal = indicators[i].Signal,
                    TakeProfit = indicators[i].TakeProfit,
                    StopLoss = indicators[i].StopLoss,
                    StartTime = indicators[i].Candle.Time,
                    EndTime = indicators[i].Candle.Time,
                    Result = 0.0
                });
            }

            foreach (var trade in openTrades)
            {
                UpdateTrade(trade, indicators[i]);

                if (trade.Running) continue;

                closedTrades.Add(trade);
            }

            openTrades.RemoveAll(ot => !ot.Running);
        }

        return closedTrades.ToArray();
    }

    private static void UpdateTrade(TradeResult trade, IndicatorBase indicator)
    {
        if (trade.Signal == Signal.Buy)
        {

            if (indicator.Candle.Bid_H >= trade.TakeProfit && indicator.Candle.Bid_L > trade.StopLoss)
            {
                CloseTrade(trade, 1, indicator.Candle.Time, indicator.Candle.Bid_H);
            }

            if (indicator.Candle.Bid_L <= trade.StopLoss && indicator.Candle.Bid_H < trade.TakeProfit)
            {
                CloseTrade(trade, -1, indicator.Candle.Time, indicator.Candle.Bid_L);
            }

            if (indicator.Candle.Bid_L <= trade.StopLoss && indicator.Candle.Bid_H >= trade.TakeProfit)
            {
                CloseTrade(trade, 0, indicator.Candle.Time, indicator.Candle.Mid_C);
            }
        }

        if (trade.Signal == Signal.Sell)
        {
            if (indicator.Candle.Ask_L <= trade.TakeProfit && indicator.Candle.Ask_H < trade.StopLoss)
            {
                CloseTrade(trade, 1, indicator.Candle.Time, indicator.Candle.Ask_L);
            }

            if (indicator.Candle.Ask_H >= trade.StopLoss && indicator.Candle.Ask_L > trade.TakeProfit)
            {
                CloseTrade(trade, -1, indicator.Candle.Time, indicator.Candle.Ask_H);
            }

            if (indicator.Candle.Ask_H >= trade.StopLoss && indicator.Candle.Ask_L <= trade.TakeProfit)
            {
                CloseTrade(trade, 0, indicator.Candle.Time, indicator.Candle.Mid_C);
            }
        }
    }

    private static void CloseTrade(TradeResult trade, double result, DateTime endTime, double triggerPrice)
    {
        trade.Running = false;
        trade.Result = result;
        trade.EndTime = endTime;
        trade.TriggerPrice = triggerPrice;
    }
}