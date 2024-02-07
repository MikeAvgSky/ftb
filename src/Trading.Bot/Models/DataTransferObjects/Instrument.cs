namespace Trading.Bot.Models.DataTransferObjects;

public class Instrument
{
    public string Name { get; }
    public string Type { get; }
    public string DisplayName { get; }
    public double PipLocation { get; }
    public int TradeUnitsPrecision { get; }
    public double MarginRate { get; }

    public Instrument(string name, string type, string displayName, int pipLocation, int tradeUnitsPrecision, double marginRate)
    {
        Name = name;
        Type = type;
        DisplayName = displayName;
        PipLocation = Math.Pow(10, pipLocation);
        TradeUnitsPrecision = tradeUnitsPrecision;
        MarginRate = marginRate;
    }
}