namespace WorkshopCartsSimulation.Core.Distributions;

public sealed class NormalVariate : IRandomVariate
{
    private readonly Random _random;
    private double? _spare;

    public NormalVariate(double mean, double standardDeviation, Random random)
    {
        if (mean <= 0)
            throw new ArgumentOutOfRangeException(nameof(mean), "Mean must be positive.");
        if (standardDeviation < 0)
            throw new ArgumentOutOfRangeException(nameof(standardDeviation), "Standard deviation cannot be negative.");
        Mean = mean;
        StandardDeviation = standardDeviation;
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public double Mean { get; }

    public double StandardDeviation { get; }

    public double Next()
    {
        while (true)
        {
            var value = Mean + StandardDeviation * NextStandardNormal();
            if (value >= 0)
                return value;
        }
    }

    private double NextStandardNormal()
    {
        if (_spare is { } spare)
        {
            _spare = null;
            return spare;
        }

        var u1 = 1.0 - _random.NextDouble();
        var u2 = _random.NextDouble();
        var radius = Math.Sqrt(-2.0 * Math.Log(u1));
        var angle = 2.0 * Math.PI * u2;
        _spare = radius * Math.Sin(angle);
        return radius * Math.Cos(angle);
    }

    public override string ToString() => FormattableString.Invariant($"norm({Mean}, {StandardDeviation})");
}
