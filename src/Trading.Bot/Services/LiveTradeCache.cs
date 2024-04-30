namespace Trading.Bot.Services;

public class LiveTradeCache
{
    public readonly Dictionary<string, LivePrice> LivePrices = new();

    public readonly Channel<LivePrice> LivePriceChannel = Channel.CreateUnbounded<LivePrice>();
}