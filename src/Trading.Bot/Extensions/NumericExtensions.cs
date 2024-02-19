namespace Trading.Bot.Extensions;

public static class NumericExtensions
{
    public static IEnumerable<double> CalcCma(this double[] sequence)
    {
        if (sequence is null)
        {
            yield break;
        }

        double total = 0;

        var count = 0;

        var length = sequence.Length;

        for (var i = 0; i < length; i++)
        {
            count++;

            total += sequence[i];

            yield return total / count;
        }
    }

    public static IEnumerable<double> CalcSma(this double[] sequence, int window)
    {
        if (sequence is null)
        {
            yield break;
        }

        var length = sequence.Length;

        if (length <= window)
        {
            yield return 0.0;
        }

        var queue = new Queue<double>(window);

        for (var i = 0; i < length; i++)
        {
            if (queue.Count == window)
            {
                queue.Dequeue();
            }

            queue.Enqueue(sequence[i]);

            yield return queue.Average();
        }
    }

    public static IEnumerable<double> CalcEma(this double[] sequence, int window)
    {
        if (sequence is null)
        {
            yield break;
        }

        var length = sequence.Length;

        if (length <= window)
        {
            yield return 0.0;
        }

        var alpha = 2.0 / (window + 1);

        var result = 0.0;

        for (var i = 0; i < length; i++)
        {
            result = i == 0
                ? sequence[i]
                : alpha * sequence[i] + (1 - alpha) * result;

            yield return result;
        }
    }

    public static IEnumerable<double> CalcRma(this double[] sequence, int window)
    {
        if (sequence is null)
        {
            yield break;
        }

        var length = sequence.Length;

        if (length <= window)
        {
            yield return 0.0;
        }

        var alpha = 1.0 / window;

        var result = 0.0;

        for (var i = 0; i < length; i++)
        {
            result = i == 0
                ? sequence[i]
                : alpha * sequence[i] + (1 - alpha) * result;

            yield return result;
        }
    }

    public static IEnumerable<double> CalcRolStdDev(this double[] sequence, int window, double std)
    {
        if (sequence is null)
        {
            yield break;
        }

        var length = sequence.Length;

        if (length <= window)
        {
            yield return 0.0;
        }

        var queue = new Queue<double>(window);

        for (var i = 0; i < length; i++)
        {
            if (queue.Count == window)
            {
                queue.Dequeue();
            }

            queue.Enqueue(sequence[i]);

            yield return queue.CalcStdDev(std);
        }
    }

    public static double CalcStdDev(this IEnumerable<double> sequence, double std)
    {
        if (sequence is null)
        {
            return 0.0;
        }

        var list = sequence.ToArray();

        var length = list.Length;

        if (length <= 1)
        {
            return 0.0;
        }

        var average = list.Average();

        double sum = 0;

        for (var i = 0; i < length; i++)
        {
            sum += Math.Pow(Math.Abs(list[i] - average), std);
        }

        return Math.Sqrt(sum / length).NaN2Zero();
    }

    public static double NaN2Zero(this double value)
        => double.IsNaN(value)
            ? 0.0
            : value;
}