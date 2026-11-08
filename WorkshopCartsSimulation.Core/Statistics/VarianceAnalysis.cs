namespace WorkshopCartsSimulation.Core.Statistics;

public sealed record VarianceAnalysis(
    double FactorVariance,
    double ResidualVariance,
    int FactorDf,
    int ResidualDf)
{
    public double F => FactorVariance / ResidualVariance;

    public double FCritical => FisherDistribution.Quantile(0.95, FactorDf, ResidualDf);

    public double PValue => FisherDistribution.UpperTailProbability(F, FactorDf, ResidualDf);

    public bool IsSignificant => F > FCritical;

    public static VarianceAnalysis OneWay(IReadOnlyList<IReadOnlyList<double>> groups)
    {
        var (levels, runs) = Validate(groups);
        var groupMeans = groups.Select(group => group.Average()).ToArray();
        var grandMean = groupMeans.Average();

        var factorSum = runs * groupMeans.Sum(mean => (mean - grandMean) * (mean - grandMean));
        var residualSum = groups.Select((group, j) => group.Sum(y => (y - groupMeans[j]) * (y - groupMeans[j]))).Sum();

        var factorDf = levels - 1;
        var residualDf = levels * (runs - 1);
        return new VarianceAnalysis(factorSum / factorDf, residualSum / residualDf, factorDf, residualDf);
    }

    public static VarianceAnalysis RandomizedBlocks(IReadOnlyList<IReadOnlyList<double>> groups)
    {
        var (levels, runs) = Validate(groups);
        var groupMeans = groups.Select(group => group.Average()).ToArray();
        var blockMeans = Enumerable.Range(0, runs).Select(s => groups.Average(group => group[s])).ToArray();
        var grandMean = groupMeans.Average();

        var factorSum = runs * groupMeans.Sum(mean => (mean - grandMean) * (mean - grandMean));
        var residualSum = 0.0;
        for (var j = 0; j < levels; j++)
        {
            for (var s = 0; s < runs; s++)
            {
                var residual = groups[j][s] - groupMeans[j] - blockMeans[s] + grandMean;
                residualSum += residual * residual;
            }
        }

        var factorDf = levels - 1;
        var residualDf = (levels - 1) * (runs - 1);
        return new VarianceAnalysis(factorSum / factorDf, residualSum / residualDf, factorDf, residualDf);
    }

    private static (int Levels, int Runs) Validate(IReadOnlyList<IReadOnlyList<double>> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        if (groups.Count < 2)
            throw new ArgumentException("At least two factor levels are required.", nameof(groups));

        var runs = groups[0].Count;
        if (runs < 2 || groups.Any(group => group.Count != runs))
            throw new ArgumentException("All levels must have the same number of runs (at least two).", nameof(groups));

        return (groups.Count, runs);
    }
}
