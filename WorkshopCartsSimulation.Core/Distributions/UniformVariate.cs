namespace WorkshopCartsSimulation.Core.Distributions;

public sealed class UniformVariate : IRandomVariate
{
    private readonly Random _random;

    public UniformVariate(double mean, double halfWidth, Random random)
    {
        if (halfWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(halfWidth), "Half-width cannot be negative.");
        if (mean - halfWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(mean), "Interval must not contain negative values.");
        Mean = mean;
        HalfWidth = halfWidth;
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public double Mean { get; }

    public double HalfWidth { get; }

    public double Next() => Mean - HalfWidth + 2.0 * HalfWidth * _random.NextDouble();

    public override string ToString() => FormattableString.Invariant($"unif({Mean}, {HalfWidth})");
}
