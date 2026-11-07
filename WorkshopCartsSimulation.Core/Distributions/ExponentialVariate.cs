namespace WorkshopCartsSimulation.Core.Distributions;

public sealed class ExponentialVariate : IRandomVariate
{
    private readonly Random _random;

    public ExponentialVariate(double mean, Random random)
    {
        if (mean <= 0)
            throw new ArgumentOutOfRangeException(nameof(mean), "Mean must be positive.");
        Mean = mean;
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public double Mean { get; }

    public double Next() => -Mean * Math.Log(1.0 - _random.NextDouble());

    public override string ToString() => FormattableString.Invariant($"exp({Mean})");
}
