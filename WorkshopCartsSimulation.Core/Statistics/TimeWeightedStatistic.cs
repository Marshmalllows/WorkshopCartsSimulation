namespace WorkshopCartsSimulation.Core.Statistics;

public sealed class TimeWeightedStatistic
{
    private double _area;
    private double _lastTime;
    private double _startTime;

    public TimeWeightedStatistic(double startTime = 0, double initialValue = 0)
    {
        _startTime = startTime;
        _lastTime = startTime;
        Value = initialValue;
        Min = initialValue;
        Max = initialValue;
    }

    public double Value { get; private set; }

    public double Min { get; private set; }

    public double Max { get; private set; }

    public void Update(double time, double newValue)
    {
        Accumulate(time);
        Value = newValue;
        Min = Math.Min(Min, newValue);
        Max = Math.Max(Max, newValue);
    }

    public double Mean(double time)
    {
        var duration = time - _startTime;
        if (duration <= 0)
            return Value;

        return Integral(time) / duration;
    }

    public double Integral(double time) => _area + Value * (time - _lastTime);

    public void Reset(double time)
    {
        _area = 0;
        _startTime = time;
        _lastTime = time;
        Min = Value;
        Max = Value;
    }

    private void Accumulate(double time)
    {
        if (time < _lastTime)
            throw new ArgumentOutOfRangeException(nameof(time), "Time cannot go backwards.");
        _area += Value * (time - _lastTime);
        _lastTime = time;
    }
}
