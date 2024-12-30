namespace Trading.Bot.Models.Indicators;

public class SimulationSummary
{
    public int Trades { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    public double Winnings { get; set; }
}