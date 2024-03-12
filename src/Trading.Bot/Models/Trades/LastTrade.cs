namespace Trading.Bot.Models.Trades;

public class LastTrade
{
    public string Instrument { get; set; }
    public Signal Signal { get; set; }
    public double TakeProfit { get; set; }

    public LastTrade(string instrument, Signal signal, double takeProfit)
    {
        Instrument = instrument;
        Signal = signal;
        TakeProfit = takeProfit;
    }
}