namespace Trading.Bot.Models.ApiResponses;

public class AccountResponse
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GuaranteedStopLossOrderMode GuaranteedStopLossOrderMode { get; set; }
    public bool HedgingEnabled { get; set; }
    public string Id { get; set; }
    public DateTime CreatedTime { get; set; }
    public string Currency { get; set; }
    public int CreatedByUserID { get; set; }
    public string Alias { get; set; }
    public double MarginRate { get; set; }
    public string LastTransactionID { get; set; }
    public double Balance { get; set; }
    public int OpenTradeCount { get; set; }
    public int OpenPositionCount { get; set; }
    public int PendingOrderCount { get; set; }
    public double Pl { get; set; }
    public double ResettablePL { get; set; }
    public string ResettablePLTime { get; set; }
    public double Financing { get; set; }
    public double Commission { get; set; }
    public double DividendAdjustment { get; set; }
    public double GuaranteedExecutionFees { get; set; }
    public double UnrealizedPL { get; set; }
    public double NAV { get; set; }
    public double MarginUsed { get; set; }
    public double MarginAvailable { get; set; }
    public double PositionValue { get; set; }
    public double MarginCloseoutUnrealizedPL { get; set; }
    public double MarginCloseoutNAV { get; set; }
    public double MarginCloseoutMarginUsed { get; set; }
    public double MarginCloseoutPositionValue { get; set; }
    public double MarginCloseoutPercent { get; set; }
    public double WithdrawalLimit { get; set; }
    public double MarginCallMarginUsed { get; set; }
    public double MarginCallPercent { get; set; }
}