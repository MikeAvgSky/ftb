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
}