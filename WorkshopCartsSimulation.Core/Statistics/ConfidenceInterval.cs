namespace WorkshopCartsSimulation.Core.Statistics;

public readonly record struct ConfidenceInterval(double Mean, double HalfWidth)
{
    public double Lower => Mean - HalfWidth;

    public double Upper => Mean + HalfWidth;

    public double RelativeHalfWidth => HalfWidth / Math.Abs(Mean);

    public static ConfidenceInterval FromSample(TallyStatistic sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        if (sample.Count < 2)
            throw new ArgumentException("At least two observations are required.", nameof(sample));

        var t = StudentQuantile975(sample.Count - 1);
        return new ConfidenceInterval(sample.Mean, t * sample.StandardDeviation / Math.Sqrt(sample.Count));
    }

    private static readonly double[] SmallSampleQuantiles =
    [
        12.706, 4.303, 3.182, 2.776, 2.571, 2.447, 2.365, 2.306, 2.262, 2.228,
        2.201, 2.179, 2.160, 2.145, 2.131, 2.120, 2.110, 2.101, 2.093, 2.086,
        2.080, 2.074, 2.069, 2.064, 2.060, 2.056, 2.052, 2.048, 2.045, 2.042,
    ];

    public static double StudentQuantile975(long degreesOfFreedom)
    {
        if (degreesOfFreedom < 1)
            throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom), "Degrees of freedom must be positive.");
        if (degreesOfFreedom <= SmallSampleQuantiles.Length)
            return SmallSampleQuantiles[degreesOfFreedom - 1];

        const double z = 1.959963984540054;
        var n = (double)degreesOfFreedom;
        var z2 = z * z;
        var z3 = z2 * z;
        var z5 = z3 * z2;
        var z7 = z5 * z2;
        return z
               + (z3 + z) / (4 * n)
               + (5 * z5 + 16 * z3 + 3 * z) / (96 * n * n)
               + (3 * z7 + 19 * z5 + 17 * z3 - 15 * z) / (384 * n * n * n);
    }
}
