namespace Trading.Bot.Extensions;

public static class MovingAverages
{
    public static IEnumerable<double> CalcCma(this IEnumerable<double> sequence)
    {
        if (sequence == null)
        {
            yield break;
        }

        double total = 0;

        var count = 0;

        foreach (var d in sequence)
        {
            count++;

            total += d;

            yield return total / count;
        }
    }

    public static IEnumerable<double> CalcSma(this IEnumerable<double> sequence, int window)
    {
        if (sequence == null)
        {
            yield break;
        }

        var queue = new Queue<double>(window);

        foreach (var d in sequence)
        {
            if (queue.Count == window)
            {
                queue.Dequeue();
            }

            queue.Enqueue(d);

            yield return queue.Average();
        }
    }

    public static IEnumerable<double> CalcEma(this IEnumerable<double> sequence, int window)
    {
        if (sequence == null)
        {
            yield break;
        }

        var list = sequence.ToArray();

        if (!list.Any())
        {
            yield return 0;
        }

        var alpha = 2.0 / (window + 1);

        var result = 0.0;

        for (var i = 0; i < list.Length; i++)
        {
            result = i == 0
                ? list[i]
                : alpha * list[i] + (1 - alpha) * result;

            yield return result;
        }
    }

    public static IEnumerable<double> CalcRma(this IEnumerable<double> sequence, int window)
    {
        if (sequence == null)
        {
            yield break;
        }

        var list = sequence.ToArray();

        if (!list.Any())
        {
            yield return 0;
        }

        var alpha = 1.0 / window;

        var result = 0.0;

        for (var i = 0; i < list.Length; i++)
        {
            result = i == 0
                ? list[i]
                : alpha * list[i] + (1 - alpha) * result;

            yield return result;
        }
    }

    public static double CalcStdDev(this IEnumerable<double> sequence, int std)
    {
        if (sequence == null)
        {
            return 0;
        }

        var list = sequence.ToArray();

        if (!list.Any())
        {
            return 0;
        }

        var average = list.Average();

        var sum = list.Sum(d => Math.Pow(d - average, std));

        return Math.Sqrt(sum / list.Length);
    }

    public static IEnumerable<double> CalcRolStdDev(this IEnumerable<double> sequence, int window, int std)
    {
        var queue = new Queue<double>(window);

        foreach (var d in sequence)
        {
            if (queue.Count == window)
            {
                queue.Dequeue();
            }

            queue.Enqueue(d);

            yield return queue.CalcStdDev(std);
        }
    }
}