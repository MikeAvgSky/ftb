namespace Trading.Bot.Models;

public class SimpleMovingAverage
{
    private readonly int _window;
    private readonly int[] _values;
    private int _index, _sum;

    public SimpleMovingAverage(int window)
    {
        _window = window;
        _values = new int[window];
    }

    public double Update(int nextInput)
    {
        _sum = _sum - _values[_index] + nextInput;

        _values[_index] = nextInput;

        _index = (_index + 1) % _window;

        return (double)_sum / _window;
    }
}