namespace Trading.Bot.Models.ApiResponses;

public class TradeResponse
{
    public double CurrentUnits { get; set; }
    public double Financing { get; set; }
    public string Id { get; set; }
    public double InitialUnits { get; set; }
    public string Instrument { get; set; }
    public DateTime OpenTime { get; set; }
    public double Price { get; set; }
    public double RealizedPL { get; set; }
    public string State { get; set; }
    public double UnrealizedPL { get; set; }
    public double MarginUsed { get; set; }
    public ClientExtensions ClientExtensions { get; set; }
}

public class ClientExtensions
{
    public string Id { get; set; }
}