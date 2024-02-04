namespace Trading.Bot.Extensions;

public static class Indicators
{
    public static IEnumerable<double> MovingAverage(this IEnumerable<double> sequence, int window)
    {
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

    public static IEnumerable<double> CumulativeMovingAverage(this IEnumerable<double> sequence)
    {
        double total = 0;

        var count = 0;

        foreach (var d in sequence)
        {
            count++;

            total += d;

            yield return total / count;
        }
    }

    public static double StandardDeviation(this IEnumerable<double> sequence, int std)
    {
        if (sequence == null)
        {
            return 0;
        }

        var list = sequence.ToList();

        if (!list.Any())
        {
            return 0;
        }

        var average = list.Average();

        var sum = list.Sum(d => Math.Pow(d - average, std));

        return Math.Sqrt(sum / list.Count);
    }

    public static IEnumerable<double> MovingStandardDeviation(this IEnumerable<double> sequence, int window, int std)
    {
        var queue = new Queue<double>(window);

        foreach (var d in sequence)
        {
            if (queue.Count == window)
            {
                queue.Dequeue();
            }

            queue.Enqueue(d);

            yield return queue.StandardDeviation(std);
        }
    }
}