﻿namespace Trading.Bot.API.Models;

public class TradeResult
{
    public bool Running { get; set; }
    public int StartIndex { get; set; }
    public decimal StartPrice { get; set; }
    public decimal TriggerPrice { get; set; }
    public Signal Signal { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal StopLoss { get; set; }
    public decimal UnrealisedPL { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal Result { get; set; }
}