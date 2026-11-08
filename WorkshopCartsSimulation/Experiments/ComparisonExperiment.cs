using System.Globalization;
using System.Text;
using WorkshopCartsSimulation.Core.Statistics;
using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class ComparisonExperiment
{
    private const int ReplicationCount = 100;
    private const string OutputFile = "comparison.csv";

    public static void Run()
    {
        var machines = WorkshopParameters.DefaultMachineCount;
        var shared = CartAssignment.Shared(3, machines);
        var pairs = CartAssignment.Dedicated(machines, [0, 1], [2, 3], [4]);
        var nearSingle = CartAssignment.Dedicated(machines, [0], [1, 2], [3, 4]);
        var overlapping = CartAssignment.Dedicated(machines, [0, 1, 2], [1, 2, 3], [2, 3, 4]);

        var variants = new (string Name, CartAssignment CartsA, CartAssignment CartsB)[]
        {
            ("V1 all shared", shared, shared),
            ("V2 dedicated {0,1}{2,3}{4}", pairs, pairs),
            ("V3 A shared, B dedicated", shared, pairs),
            ("V4 A dedicated, B shared", pairs, shared),
            ("V5 dedicated {0}{1,2}{3,4}", nearSingle, nearSingle),
            ("V6 overlapping {0-2}{1-3}{2-4}", overlapping, overlapping),
        };

        var results = variants
            .Select(variant => Replications.Run(
                new WorkshopParameters { CartsA = variant.CartsA, CartsB = variant.CartsB }, ReplicationCount))
            .ToList();
        var baseline = results[0];

        Console.WriteLine($"Cart assignment comparison: {ReplicationCount} runs per variant (common random numbers), " +
                          $"T = {ExperimentSettings.SimulationTime}, warm-up = {ExperimentSettings.WarmUpTime}");
        Console.WriteLine();

        var headers = new[]
        {
            "Variant", "Profit/t", "+/-", "Diff vs V1", "+/-", "Significant", "Throughput", "Lq", "Processing",
            "Cart A", "Cart B",
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
            summary.Processing.ToString("F3"),
            summary.CartA.ToString("F3"),
            summary.CartB.ToString("F3"),
        ]).ToList();
        TablePrinter.Print(headers, rows);

        var best = summaries.MaxBy(summary => summary.Profit.Mean)!;
        Console.WriteLine();
        Console.WriteLine($"Best variant: {best.Name} ({best.Profit.Mean:F4} +/- {best.Profit.HalfWidth:F4} per time unit)");

        WriteCsv(summaries);
        Console.WriteLine($"Summary saved to {Path.GetFullPath(OutputFile)}");
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
            results.Average(r => r.MachineProcessing.Average()),
            results.Average(r => r.CartAUtilization),
            results.Average(r => r.CartBUtilization));
    }

    private static void WriteCsv(IEnumerable<VariantSummary> summaries)
    {
        var builder = new StringBuilder("variant;profit_rate;profit_half_width;diff_vs_v1;diff_half_width;" +
                                        "throughput;queue_length;processing;cart_a;cart_b");
        builder.AppendLine();
        foreach (var s in summaries)
        {
            var values = new[]
            {
                s.Profit.Mean, s.Profit.HalfWidth, s.Difference?.Mean ?? 0, s.Difference?.HalfWidth ?? 0,
                s.Throughput, s.QueueLength, s.Processing, s.CartA, s.CartB,
            };
            builder.AppendLine(s.Name + ";" + string.Join(";", values.Select(v => v.ToString("G6", CultureInfo.InvariantCulture))));
        }

        File.WriteAllText(OutputFile, builder.ToString());
    }

    private sealed record VariantSummary(
        string Name,
        ConfidenceInterval Profit,
        ConfidenceInterval? Difference,
        double Throughput,
        double QueueLength,
        double Processing,
        double CartA,
        double CartB);
}
