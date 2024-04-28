namespace Trading.Bot.Models.DataTransferObjects;

public class Instrument
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string DisplayName { get; set; }
    public double PipLocation { get; set; }
    public int DisplayPrecision { get; set; }
    public int TradeUnitsPrecision { get; set; }
    public double MarginRate { get; set; }

    public Instrument(InstrumentResponse instrument)
    {
        Name = instrument.Name;
        Type = instrument.Type;
        DisplayName = instrument.DisplayName;
        PipLocation = Math.Pow(10, instrument.PipLocation);
        DisplayPrecision = instrument.DisplayPrecision;
        TradeUnitsPrecision = instrument.TradeUnitsPrecision;
        MarginRate = instrument.MarginRate;
    }
}