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
    public decimal MarginRate { get; set; }
    public string LastTransactionID { get; set; }
    public decimal Balance { get; set; }
    public int OpenTradeCount { get; set; }
    public int OpenPositionCount { get; set; }
    public int PendingOrderCount { get; set; }
    public decimal Pl { get; set; }
    public decimal ResettablePL { get; set; }
    public string ResettablePLTime { get; set; }
    public decimal Financing { get; set; }
    public decimal Commission { get; set; }
    public decimal DividendAdjustment { get; set; }
    public decimal GuaranteedExecutionFees { get; set; }
    public decimal UnrealizedPL { get; set; }
    public decimal NAV { get; set; }
    public decimal MarginUsed { get; set; }
    public decimal MarginAvailable { get; set; }
    public decimal PositionValue { get; set; }
    public decimal MarginCloseoutUnrealizedPL { get; set; }
    public decimal MarginCloseoutNAV { get; set; }
    public decimal MarginCloseoutMarginUsed { get; set; }
    public decimal MarginCloseoutPositionValue { get; set; }
    public decimal MarginCloseoutPercent { get; set; }
    public decimal WithdrawalLimit { get; set; }
    public decimal MarginCallMarginUsed { get; set; }
    public decimal MarginCallPercent { get; set; }
}