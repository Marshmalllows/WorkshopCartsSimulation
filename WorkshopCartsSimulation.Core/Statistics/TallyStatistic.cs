namespace WorkshopCartsSimulation.Core.Statistics;

public sealed class TallyStatistic
{
    private double _mean;
    private double _sumOfSquaredDeviations;

    public long Count { get; private set; }

    public double Mean => Count > 0 ? _mean : double.NaN;

    public double Sum => _mean * Count;

    public double Variance => Count > 1 ? _sumOfSquaredDeviations / (Count - 1) : double.NaN;

    public double StandardDeviation => Math.Sqrt(Variance);

    public double Min { get; private set; } = double.NaN;

    public double Max { get; private set; } = double.NaN;

    public void Add(double value)
    {
        Count++;
        var delta = value - _mean;
        _mean += delta / Count;
        _sumOfSquaredDeviations += delta * (value - _mean);

        if (Count == 1)
        {
            Min = value;
            Max = value;
        }
        else
        {
            Min = Math.Min(Min, value);
            Max = Math.Max(Max, value);
        }
    }

    public void Reset()
    {
        Count = 0;
        _mean = 0;
        _sumOfSquaredDeviations = 0;
        Min = double.NaN;
        Max = double.NaN;
    }
}
