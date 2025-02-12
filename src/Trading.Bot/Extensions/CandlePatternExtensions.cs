namespace Trading.Bot.Extensions;

public static class CandlePatternExtensions
{
    private const double HangingManBody = 15.0;
    private const double HangingManHeight = 75.0;
    private const double ShootingStarHeight = 75.0;
    private const double SpinningTopMin = 40.0;
    private const double SpinningTopMax = 60.0;
    private const double Marubozu = 98.0;
    private const double EngulfingFactor = 1.2;
    private const double TweezerBody = 15.0;
    private const double TweezerTopBody = 40.0;
    private const double TweezerBottomBody = 60.0;
    private const double TweezerHLPercentageDifference = 0.01;
    private const double MorningStarPrev2Body = 90.0;
    private const double MorningStarPrevBody = 10.0;

    public static bool IsHangingMan(this Candle candle)
    {
        if (candle is null) return false;

        return candle.BodyBottomPercentage > HangingManHeight &&
               candle.BodyPercentage < HangingManBody;
    }

    public static bool IsShootingStar(this Candle candle)
    {
        if (candle is null) return false;

        return candle.BodyTopPercentage < ShootingStarHeight &&
               candle.BodyPercentage < HangingManBody;
    }

    public static bool IsSpinningTop(this Candle candle)
    {
        if (candle is null) return false;

        return candle.BodyTopPercentage < SpinningTopMax &&
               candle.BodyBottomPercentage > SpinningTopMin &&
               candle.BodyPercentage < HangingManBody;
    }

    public static bool IsMarubozu(this Candle candle) => candle.BodyPercentage > Marubozu;

    public static bool IsEngulfingCandle(this Candle candle, Candle prevCandle)
    {
        if (candle is null || prevCandle is null) return false;

        return candle.Direction != prevCandle.Direction &&
               candle.BodySize > prevCandle.BodySize * EngulfingFactor;
    }

    public static bool IsTweezerTop(this Candle candle, Candle prevCandle)
    {
        if (candle is null || prevCandle is null) return false;

        var lowChange = (candle.Mid_L - prevCandle.Mid_L) / prevCandle.Mid_L * 100;

        var highChange = (candle.Mid_H - prevCandle.Mid_H) / prevCandle.Mid_H * 100;

        var bodyChange = (candle.BodySize - prevCandle.BodySize) / prevCandle.BodySize * 100;

        return Math.Abs(bodyChange) < TweezerBody && candle.Direction == -1 &&
               candle.Direction != prevCandle.Direction &&
               Math.Abs(lowChange) < TweezerHLPercentageDifference &&
               Math.Abs(highChange) < TweezerHLPercentageDifference &&
               candle.BodyTopPercentage < TweezerTopBody;
    }

    public static bool IsTweezerBottom(this Candle candle, Candle prevCandle)
    {
        if (candle is null || prevCandle is null) return false;

        var lowChange = (candle.Mid_L - prevCandle.Mid_L) / prevCandle.Mid_L * 100;

        var highChange = (candle.Mid_H - prevCandle.Mid_H) / prevCandle.Mid_H * 100;

        var bodyChange = (candle.BodySize - prevCandle.BodySize) / prevCandle.BodySize * 100;

        return Math.Abs(bodyChange) < TweezerBody && candle.Direction == 1 &&
               candle.Direction != prevCandle.Direction &&
               Math.Abs(lowChange) < TweezerHLPercentageDifference &&
               Math.Abs(highChange) < TweezerHLPercentageDifference &&
               candle.BodyBottomPercentage > TweezerBottomBody;
    }

    public static bool IsMorningStar(this Candle candle, Candle[] lastTwoCandles, int direction = 1)
    {
        if (candle is null || lastTwoCandles is null || lastTwoCandles.Length != 2) return false;

        var prev2Candle = lastTwoCandles.OrderBy(c => c.Time).First();

        var prevCandle = lastTwoCandles.OrderBy(c => c.Time).Last();

        return prev2Candle.BodyPercentage > MorningStarPrev2Body &&
               prevCandle.BodyPercentage < MorningStarPrevBody &&
               candle.Direction == direction && prev2Candle.Direction != direction &&
               (direction == 1 && candle.Mid_C > prev2Candle.MidPoint ||
                direction == -1 && candle.Mid_C < prev2Candle.MidPoint);
    }

    public static bool HigherHighs(this Candle[] candles)
    {
        var higherHighs = 0;

        var latestHigh = candles[0].Mid_H;

        for (var i = 1; i < candles.Length; i++)
        {
            if (!IsSwingHigh(candles, i) || !(candles[i].Mid_H > latestHigh)) continue;

            latestHigh = candles[i].Mid_H;

            higherHighs++;
        }

        return higherHighs > 1;
    }

    public static bool LowerHighs(this Candle[] candles)
    {
        var lowerHighs = 0;

        var latestHigh = candles[0].Mid_H;

        for (var i = 1; i < candles.Length; i++)
        {
            if (!IsSwingLow(candles, i) || !(candles[i].Mid_H < latestHigh)) continue;

            latestHigh = candles[i].Mid_H;

            lowerHighs++;
        }

        return lowerHighs > 1;
    }

    public static bool HigherLows(this Candle[] candles)
    {
        var higherLows = 0;

        var latestLow = candles[0].Mid_L;

        for (var i = 1; i < candles.Length; i++)
        {
            if (!IsSwingHigh(candles, i) || !(candles[i].Mid_L > latestLow)) continue;

            latestLow = candles[i].Mid_L;

            higherLows++;
        }

        return higherLows > 1;
    }

    public static bool LowerLows(this Candle[] candles)
    {
        var lowerLows = 0;

        var latestLow = candles[0].Mid_L;

        for (var i = 1; i < candles.Length; i++)
        {
            if (!IsSwingLow(candles, i) || !(candles[i].Mid_L < latestLow)) continue;

            latestLow = candles[i].Mid_L;

            lowerLows++;
        }

        return lowerLows > 1;
    }

    private static bool IsSwingHigh(Candle[] candles, int index)
    {
        if (index < 1 || index >= candles.Length - 1) return false;

        return candles[index].Mid_H > candles[index - 1].Mid_H && candles[index].Mid_H > candles[index + 1].Mid_H;
    }

    private static bool IsSwingLow(Candle[] candles, int index)
    {
        if (index < 1 || index >= candles.Length - 1) return false;

        return candles[index].Mid_L < candles[index - 1].Mid_L && candles[index].Mid_L < candles[index + 1].Mid_L;
    }
}