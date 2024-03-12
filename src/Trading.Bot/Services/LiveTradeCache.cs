namespace Trading.Bot.Services;

public class LiveTradeCache
{
    public readonly Dictionary<string, LivePrice> LivePrices = new();

    public readonly Dictionary<string, LastTrade> LastTrades = new();

    public readonly Queue<LivePrice> LivePriceQueue = new();

    public void AddToQueue(LivePrice price)
    {
        LivePriceQueue.Enqueue(price);
    }

    public void AddToDictionary(LivePrice price)
    {
        LivePrices[price.Instrument] = price;
    }

    public void AddToDictionary(LastTrade lastTrade)
    {
        LastTrades[lastTrade.Instrument] = lastTrade;
    }
}