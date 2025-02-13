namespace Trading.Bot.Models.ApiResponses;

public class TradeResponse
{
    public decimal CurrentUnits { get; set; }
    public decimal Financing { get; set; }
    public string Id { get; set; }
    public decimal InitialUnits { get; set; }
    public string Instrument { get; set; }
    public DateTime OpenTime { get; set; }
    public decimal Price { get; set; }
    public decimal RealizedPL { get; set; }
    public string State { get; set; }
    public decimal UnrealizedPL { get; set; }
    public decimal MarginUsed { get; set; }
    public ClientExtensions ClientExtensions { get; set; }
    public TakeProfit TakeProfitOrder { get; set; }
    public StopLoss StopLossOrder { get; set; }
    public TrailingStopLoss TrailingStopLossOrder { get; set; }
}

public class ClientExtensions
{
    public string Id { get; set; }
}