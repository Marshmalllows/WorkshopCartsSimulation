namespace WorkshopCartsSimulation.Core.Statistics;

public static class FisherDistribution
{
    private static readonly double[] LanczosCoefficients =
    [
        676.5203681218851, -1259.1392167224028, 771.32342877765313, -176.61502916214059,
        12.507343278686905, -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7,
    ];

    public static double UpperTailProbability(double f, double numeratorDf, double denominatorDf)
    {
        if (numeratorDf <= 0 || denominatorDf <= 0)
            throw new ArgumentOutOfRangeException(nameof(numeratorDf), "Degrees of freedom must be positive.");
        if (f <= 0)
            return 1;

        var x = denominatorDf / (denominatorDf + numeratorDf * f);
        return RegularizedIncompleteBeta(denominatorDf / 2, numeratorDf / 2, x);
    }

    public static double Quantile(double probability, double numeratorDf, double denominatorDf)
    {
        if (probability <= 0 || probability >= 1)
            throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be in (0, 1).");

        var upperTail = 1 - probability;
        var low = 0.0;
        var high = 1.0;
        while (UpperTailProbability(high, numeratorDf, denominatorDf) > upperTail)
            high *= 2;

        for (var i = 0; i < 200; i++)
        {
            var middle = (low + high) / 2;
            if (UpperTailProbability(middle, numeratorDf, denominatorDf) > upperTail)
                low = middle;
            else
                high = middle;
        }

        return (low + high) / 2;
    }

    private static double RegularizedIncompleteBeta(double a, double b, double x)
    {
        if (x <= 0)
            return 0;
        if (x >= 1)
            return 1;

        var front = Math.Exp(LogGamma(a + b) - LogGamma(a) - LogGamma(b) + a * Math.Log(x) + b * Math.Log(1 - x));
        if (x < (a + 1) / (a + b + 2))
            return front * BetaContinuedFraction(a, b, x) / a;

        return 1 - front * BetaContinuedFraction(b, a, 1 - x) / b;
    }

    private static double BetaContinuedFraction(double a, double b, double x)
    {
        const double tiny = 1e-300;
        const double epsilon = 1e-15;
        var c = 1.0;
        var d = 1 - (a + b) * x / (a + 1);
        d = Math.Abs(d) < tiny ? tiny : d;
        d = 1 / d;
        var result = d;

        for (var m = 1; m <= 1000; m++)
        {
            var m2 = 2 * m;
            var numerator = m * (b - m) * x / ((a + m2 - 1) * (a + m2));
            d = 1 + numerator * d;
            d = Math.Abs(d) < tiny ? tiny : d;
            c = 1 + numerator / c;
            c = Math.Abs(c) < tiny ? tiny : c;
            d = 1 / d;
            result *= d * c;

            numerator = -(a + m) * (a + b + m) * x / ((a + m2) * (a + m2 + 1));
            d = 1 + numerator * d;
            d = Math.Abs(d) < tiny ? tiny : d;
            c = 1 + numerator / c;
            c = Math.Abs(c) < tiny ? tiny : c;
            d = 1 / d;
            var delta = d * c;
            result *= delta;
            if (Math.Abs(delta - 1) < epsilon)
                break;
        }

        return result;
    }

    private static double LogGamma(double x)
    {
        if (x < 0.5)
            return Math.Log(Math.PI / Math.Abs(Math.Sin(Math.PI * x))) - LogGamma(1 - x);

        x -= 1;
        var sum = 0.99999999999980993;
        for (var i = 0; i < LanczosCoefficients.Length; i++)
            sum += LanczosCoefficients[i] / (x + i + 1);

        var t = x + LanczosCoefficients.Length - 0.5;
        return 0.5 * Math.Log(2 * Math.PI) + (x + 0.5) * Math.Log(t) - t + Math.Log(sum);
    }
}
