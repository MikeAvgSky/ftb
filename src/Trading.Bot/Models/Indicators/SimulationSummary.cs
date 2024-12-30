namespace Trading.Bot.Models.Indicators;

public class SimulationSummary
{
    public int Candles { get; set; }
    public int Trades { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Unknowns { get; set; }
    public double WinRate { get; set; }
    public int TradeRisk { get; set; }
    public double Winnings { get; set; }
}