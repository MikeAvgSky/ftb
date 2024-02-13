﻿namespace Trading.Bot.Models.Indicators;

public class KeltnerResult : IndicatorBase
{
    public double Ema { get; set; }
    public double UpperBand { get; set; }
    public double LowerBand { get; set; }
}