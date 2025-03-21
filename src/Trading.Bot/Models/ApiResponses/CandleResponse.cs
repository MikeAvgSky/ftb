﻿namespace Trading.Bot.Models.ApiResponses;

public class CandleResponse
{
    public string Instrument { get; set; }
    public string Granularity { get; set; }
    public CandleData[] Candles { get; set; }
}

public class CandleData
{
    public bool Complete { get; set; }
    public int Volume { get; set; }
    public DateTime Time { get; set; }
    public CandlestickData Bid { get; set; } = new();
    public CandlestickData Mid { get; set; } = new();
    public CandlestickData Ask { get; set; } = new();
}

public class CandlestickData
{
    public decimal O { get; set; }
    public decimal H { get; set; }
    public decimal L { get; set; }
    public decimal C { get; set; }
}