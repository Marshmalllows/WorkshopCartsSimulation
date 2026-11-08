using System.Globalization;
using System.Text;
using WorkshopCartsSimulation.Core.Statistics;
using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class ComparisonExperiment
{
    private const int ReplicationCount = 100;
    private const double DifferenceAccuracy = 0.005;
    private const string SummaryFile = "comparison.csv";
    private const string RunsFile = "comparison_runs.csv";

    public static void Run()
    {
        var variants = AssignmentVariants.All;
        var results = variants
            .Select(variant => Replications.Run(variant.Parameters(), ReplicationCount))
            .ToList();
        var baseline = results[0];

        Console.WriteLine($"Cart assignment comparison: {ReplicationCount} runs per variant (common random numbers), " +
                          $"T = {ExperimentSettings.SimulationTime}, warm-up = {ExperimentSettings.WarmUpTime}");
        Console.WriteLine();

        var headers = new[]
        {
            "Variant", "Profit/t", "+/-", "Diff vs V1", "+/-", "Significant", "Throughput", "Lq", "W", "Processing",
            "Blocked", "Cart A", "Cart B",
        };
        var summaries = variants.Select((variant, i) => Summarize(variant.Name, results[i], baseline)).ToList();
        var rows = summaries.Select(summary => (IReadOnlyList<string>)
        [
            summary.Name,
            summary.Profit.Mean.ToString("F4"),
            summary.Profit.HalfWidth.ToString("F4"),
            summary.Difference is { } d ? d.Mean.ToString("+0.0000;-0.0000") : "-",
            summary.Difference is { } h ? h.HalfWidth.ToString("F4") : "-",
            summary.Difference is { } s ? (s.Lower > 0 || s.Upper < 0 ? "yes" : "no") : "-",
            summary.Throughput.ToString("F5"),
            summary.QueueLength.ToString("F1"),
            summary.TimeInSystem.ToString("F0"),
            summary.Processing.ToString("F3"),
            summary.Blocked.ToString("F3"),
            summary.CartA.ToString("F3"),
            summary.CartB.ToString("F3"),
        ]).ToList();
        TablePrinter.Print(headers, rows);

        var best = summaries.MaxBy(summary => summary.Profit.Mean)!;
        Console.WriteLine();
        Console.WriteLine($"Best variant: {best.Name} ({best.Profit.Mean:F4} +/- {best.Profit.HalfWidth:F4} per time unit)");

        Console.WriteLine();
        Console.WriteLine("Required runs for the difference with V1 by the Chebyshev inequality: " +
                          $"p = 20 * (s / e)^2, e = {DifferenceAccuracy}");
        PrintDifferenceRuns(variants, results);

        Console.WriteLine();
        Console.WriteLine("Analysis of variance (factor: cart assignment variant)");
        PrintAnova(variants, results);

        WriteSummaryCsv(summaries);
        WriteRunsCsv(variants, results);
        Console.WriteLine();
        Console.WriteLine($"Summary saved to {Path.GetFullPath(SummaryFile)}");
        Console.WriteLine($"Per-run values saved to {Path.GetFullPath(RunsFile)}");
    }

    private static void PrintDifferenceRuns(IReadOnlyList<AssignmentVariant> variants,
        IReadOnlyList<IReadOnlyList<WorkshopResult>> results)
    {
        var headers = new[] { "Difference", "Mean", "Std dev", "Accuracy e", "Runs p" };
        var rows = Enumerable.Range(1, variants.Count - 1).Select(i =>
        {
            var difference = new TallyStatistic();
            for (var run = 0; run < results[i].Count; run++)
                difference.Add(results[i][run].ProfitPerTimeUnit - results[0][run].ProfitPerTimeUnit);
            var runs = (int)Math.Ceiling(20 * Math.Pow(difference.StandardDeviation / DifferenceAccuracy, 2));
            return (IReadOnlyList<string>)
            [
                $"{variants[i].Code} - {variants[0].Code}",
                difference.Mean.ToString("+0.0000;-0.0000"),
                difference.StandardDeviation.ToString("F4"),
                DifferenceAccuracy.ToString("F3"),
                runs.ToString(),
            ];
        }).ToList();
        TablePrinter.Print(headers, rows);
    }

    private static void PrintAnova(IReadOnlyList<AssignmentVariant> variants, IReadOnlyList<IReadOnlyList<WorkshopResult>> results)
    {
        var dedicated = Enumerable.Range(1, variants.Count - 1).ToArray();
        var cases = new (string Response, string Levels, string Method, Func<WorkshopResult, double> Selector, int[] Indexes,
            bool Blocked)[]
        {
            ("Profit/t", "V1-V6", "one-way", r => r.ProfitPerTimeUnit, Enumerable.Range(0, variants.Count).ToArray(), false),
            ("Profit/t", "V1-V6", "blocks (runs)", r => r.ProfitPerTimeUnit, Enumerable.Range(0, variants.Count).ToArray(), true),
            ("Profit/t", "V2-V6", "blocks (runs)", r => r.ProfitPerTimeUnit, dedicated, true),
            ("W", "V1-V6", "one-way", r => r.MeanTimeInSystem, Enumerable.Range(0, variants.Count).ToArray(), false),
            ("W", "V1-V6", "blocks (runs)", r => r.MeanTimeInSystem, Enumerable.Range(0, variants.Count).ToArray(), true),
        };

        var headers = new[] { "Response", "Levels", "Method", "D_F", "D_R", "F", "df", "F crit", "p-value", "Effect" };
        var rows = cases.Select(c =>
        {
            var groups = c.Indexes.Select(i => (IReadOnlyList<double>)results[i].Select(c.Selector).ToArray()).ToList();
            var anova = c.Blocked ? VarianceAnalysis.RandomizedBlocks(groups) : VarianceAnalysis.OneWay(groups);
            return (IReadOnlyList<string>)
            [
                c.Response,
                c.Levels,
                c.Method,
                anova.FactorVariance.ToString("G4"),
                anova.ResidualVariance.ToString("G4"),
                anova.F.ToString("F2"),
                $"({anova.FactorDf}; {anova.ResidualDf})",
                anova.FCritical.ToString("F3"),
                anova.PValue < 1e-4 ? "< 0.0001" : anova.PValue.ToString("F4"),
                anova.IsSignificant ? "significant" : "not significant",
            ];
        }).ToList();
        TablePrinter.Print(headers, rows);
    }

    private static VariantSummary Summarize(string name, IReadOnlyList<WorkshopResult> results,
        IReadOnlyList<WorkshopResult> baseline)
    {
        var profit = new TallyStatistic();
        var difference = new TallyStatistic();
        for (var i = 0; i < results.Count; i++)
        {
            profit.Add(results[i].ProfitPerTimeUnit);
            difference.Add(results[i].ProfitPerTimeUnit - baseline[i].ProfitPerTimeUnit);
        }

        var isBaseline = ReferenceEquals(results, baseline);
        return new VariantSummary(
            name,
            ConfidenceInterval.FromSample(profit),
            isBaseline ? null : ConfidenceInterval.FromSample(difference),
            results.Average(r => r.ProcessedUnits / r.ObservationTime),
            results.Average(r => r.MeanQueueLength),
            results.Average(r => r.MeanTimeInSystem),
            results.Average(r => r.MachineProcessing.Average()),
            results.Average(r => r.MachineBlocked.Average()),
            results.Average(r => r.CartAUtilization),
            results.Average(r => r.CartBUtilization));
    }

    private static void WriteSummaryCsv(IEnumerable<VariantSummary> summaries)
    {
        var builder = new StringBuilder("variant;profit_rate;profit_half_width;diff_vs_v1;diff_half_width;" +
                                        "throughput;queue_length;time_in_system;processing;blocked;cart_a;cart_b");
        builder.AppendLine();
        foreach (var s in summaries)
        {
            var values = new[]
            {
                s.Profit.Mean, s.Profit.HalfWidth, s.Difference?.Mean ?? 0, s.Difference?.HalfWidth ?? 0,
                s.Throughput, s.QueueLength, s.TimeInSystem, s.Processing, s.Blocked, s.CartA, s.CartB,
            };
            builder.AppendLine(s.Name + ";" + string.Join(";", values.Select(v => v.ToString("G6", CultureInfo.InvariantCulture))));
        }

        File.WriteAllText(SummaryFile, builder.ToString());
    }

    private static void WriteRunsCsv(IReadOnlyList<AssignmentVariant> variants, IReadOnlyList<IReadOnlyList<WorkshopResult>> results)
    {
        var builder = new StringBuilder("run;" + string.Join(";", variants.Select(v => v.Code + "_profit_rate")) + ";" +
                                        string.Join(";", variants.Select(v => v.Code + "_time_in_system")));
        builder.AppendLine();
        for (var run = 0; run < results[0].Count; run++)
        {
            var values = results.Select(r => r[run].ProfitPerTimeUnit)
                .Concat(results.Select(r => r[run].MeanTimeInSystem));
            builder.AppendLine((run + 1) + ";" +
                               string.Join(";", values.Select(v => v.ToString("G6", CultureInfo.InvariantCulture))));
        }

        File.WriteAllText(RunsFile, builder.ToString());
    }

    private sealed record VariantSummary(
        string Name,
        ConfidenceInterval Profit,
        ConfidenceInterval? Difference,
        double Throughput,
        double QueueLength,
        double TimeInSystem,
        double Processing,
        double Blocked,
        double CartA,
        double CartB);
}
