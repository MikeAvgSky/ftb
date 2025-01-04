namespace Trading.Bot.Models.Indicators;

public class SimulationSummary
{
    public int Days { get; set; }
    public int Candles { get; set; }
    public int Trades { get; set; }
    public int Wins { get; set; }
    public int WinsBuy { get; set; }
    public int WinsSell { get; set; }
    public int Losses { get; set; }
    public int LossesBuy { get; set; }
    public int LossesSell { get; set; }
    public int Unknown { get; set; }
    public int UnknownBuy { get; set; }
    public int UnknownSell { get; set; }
    public double WinRate { get; set; }
    public int TradeRisk { get; set; }
    public double Winnings { get; set; }
}