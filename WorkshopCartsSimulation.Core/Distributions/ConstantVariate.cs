namespace WorkshopCartsSimulation.Core.Distributions;

public sealed class ConstantVariate : IRandomVariate
{
    public ConstantVariate(double value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Delay cannot be negative.");
        Value = value;
    }

    public double Value { get; }

    public double Mean => Value;

    public double Next() => Value;

    public override string ToString() => FormattableString.Invariant($"{Value}");
}
