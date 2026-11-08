using WorkshopCartsSimulation.Core.Statistics;
using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class ReplicationExperiment
{
    private const int MaxReplications = 50;
    private const double RequiredHalfWidth = 0.01;
    private static readonly int[] Checkpoints = [3, 5, 10, 15, 20, 30, 40, 50];

    public static void Run()
    {
        var results = Replications.Run(new WorkshopParameters(), MaxReplications);

        Console.WriteLine($"Replication analysis: up to {MaxReplications} runs, T = {ExperimentSettings.SimulationTime}, " +
                          $"warm-up = {ExperimentSettings.WarmUpTime}, seeds 1..{MaxReplications}");
        Console.WriteLine("Output: profit per time unit (95% confidence interval)");
        Console.WriteLine();

        var headers = new[] { "Runs", "Mean", "Std dev", "Half-width", "Relative", "CI" };
        var rows = Checkpoints.Select(count =>
        {
            var sample = Collect(results.Take(count));
            var interval = ConfidenceInterval.FromSample(sample);
            return (IReadOnlyList<string>)
            [
                count.ToString(),
                interval.Mean.ToString("F4"),
                sample.StandardDeviation.ToString("F4"),
                interval.HalfWidth.ToString("F4"),
                interval.RelativeHalfWidth.ToString("P2"),
                $"[{interval.Lower:F4}; {interval.Upper:F4}]",
            ];
        }).ToList();
        TablePrinter.Print(headers, rows);

        var all = Collect(results);
        var t = ConfidenceInterval.StudentQuantile975(all.Count - 1);
        var required = (int)Math.Ceiling(Math.Pow(t * all.StandardDeviation / RequiredHalfWidth, 2));
        Console.WriteLine();
        Console.WriteLine($"Required runs for half-width {RequiredHalfWidth}: n = (t * s / e)^2 = " +
                          $"({t:F3} * {all.StandardDeviation:F4} / {RequiredHalfWidth})^2 = {required}");
    }

    private static TallyStatistic Collect(IEnumerable<WorkshopResult> results)
    {
        var sample = new TallyStatistic();
        foreach (var result in results)
            sample.Add(result.ProfitPerTimeUnit);
        return sample;
    }
}
