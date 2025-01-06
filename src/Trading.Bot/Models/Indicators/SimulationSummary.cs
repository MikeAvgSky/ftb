namespace Trading.Bot.Models.Indicators;

public class SimulationSummary
{
    public int Days { get; set; }
    public int Candles { get; set; }
    public int Trades { get; set; }
    public int Wins { get; set; }
    public int BuyWins { get; set; }
    public int SellWins { get; set; }
    public int Losses { get; set; }
    public int BuyLosses { get; set; }
    public int SellLosses { get; set; }
    public int Unknown { get; set; }
    public double WinRate { get; set; }
    public int TradeRisk { get; set; }
    public double Winnings { get; set; }
}