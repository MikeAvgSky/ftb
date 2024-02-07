namespace Trading.Bot.Models.ApiResponses;

public class InstrumentResponse
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string DisplayName { get; set; }
    public int PipLocation { get; set; }
    public int DisplayPrecision { get; set; }
    public int TradeUnitsPrecision { get; set; }
    public string MinimumTradeSize { get; set; }
    public double MaximumTrailingStopDistance { get; set; }
    public double MinimumTrailingStopDistance { get; set; }
    public string MaximumPositionSize { get; set; }
    public double MaximumOrderUnits { get; set; }
    public double MarginRate { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GuaranteedStopLossOrderMode GuaranteedStopLossOrderMode { get; set; }
    public string MinimumGuaranteedStopLossDistance { get; set; }
    public double GuaranteedStopLossOrderExecutionPremium { get; set; }
    public GuaranteedStopLossOrderLevelRestriction GuaranteedStopLossOrderLevelRestriction { get; set; } = new();
    public Tag[] Tags { get; set; }
    public Financing Financing { get; set; } = new();
}

public class GuaranteedStopLossOrderLevelRestriction
{
    public double Volume { get; set; }
    public double PriceRange { get; set; }
}

public class Financing
{
    public double LongRate { get; set; }
    public double ShortRate { get; set; }
    public FinancingDaysOfWeek[] FinancingDaysOfWeek { get; set; }
}

public class FinancingDaysOfWeek
{
    public string DayOfWeek { get; set; }
    public int DaysCharged { get; set; }
}

public class Tag
{
    public string Type { get; set; }
    public string Name { get; set; }
}