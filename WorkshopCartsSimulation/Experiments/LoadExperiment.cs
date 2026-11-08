using System.Globalization;
using System.Text;
using WorkshopCartsSimulation.Core.Statistics;
using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class LoadExperiment
{
    private const int ReplicationCount = 30;
    private const string OutputFile = "load.csv";
    private static readonly double[] InterarrivalTimes = [90, 95, 99, 105, 110, 120, 150, 200];
    private static readonly string[] VariantCodes = ["V1", "V2", "V6"];

    public static void Run()
    {
        var variants = AssignmentVariants.All.Where(variant => VariantCodes.Contains(variant.Code)).ToList();
        var points = InterarrivalTimes
            .SelectMany(interarrival => variants.Select(variant => (Interarrival: interarrival, Variant: variant)))
            .Select(point =>
            {
                var parameters = point.Variant.Parameters(new WorkshopParameters { MeanInterarrivalTime = point.Interarrival });
                return new LoadPoint(point.Interarrival, point.Variant.Code, Replications.Run(parameters, ReplicationCount));
            })
            .ToList();

        Console.WriteLine($"Influence of the arrival rate: {ReplicationCount} runs per point, " +
                          $"T = {ExperimentSettings.SimulationTime}, warm-up = {ExperimentSettings.WarmUpTime}");
        Console.WriteLine();

        var headers = new[] { "Interarrival", "Variant", "Lambda", "Throughput", "Lq", "W", "Processing", "Profit/t", "+/-" };
        var rows = points.Select(point => (IReadOnlyList<string>)
        [
            point.Interarrival.ToString("F0"),
            point.Variant,
            (1 / point.Interarrival).ToString("F5"),
            point.Throughput.ToString("F5"),
            point.QueueLength.ToString("F1"),
            point.TimeInSystem.ToString("F0"),
            point.Processing.ToString("F3"),
            point.Profit.Mean.ToString("F4"),
            point.Profit.HalfWidth.ToString("F4"),
        ]).ToList();
        TablePrinter.Print(headers, rows);

        WriteCsv(points);
        Console.WriteLine();
        Console.WriteLine($"Results saved to {Path.GetFullPath(OutputFile)}");
    }

    private static void WriteCsv(IEnumerable<LoadPoint> points)
    {
        var builder = new StringBuilder("interarrival;variant;throughput;queue_length;time_in_system;processing;" +
                                        "profit_rate;profit_half_width");
        builder.AppendLine();
        foreach (var p in points)
        {
            var values = new[] { p.Throughput, p.QueueLength, p.TimeInSystem, p.Processing, p.Profit.Mean, p.Profit.HalfWidth };
            builder.AppendLine(p.Interarrival.ToString(CultureInfo.InvariantCulture) + ";" + p.Variant + ";" +
                               string.Join(";", values.Select(v => v.ToString("G6", CultureInfo.InvariantCulture))));
        }

        File.WriteAllText(OutputFile, builder.ToString());
    }

    private sealed class LoadPoint(double interarrival, string variant, IReadOnlyList<WorkshopResult> results)
    {
        public double Interarrival { get; } = interarrival;

        public string Variant { get; } = variant;

        public double Throughput { get; } = results.Average(r => r.ProcessedUnits / r.ObservationTime);

        public double QueueLength { get; } = results.Average(r => r.MeanQueueLength);

        public double TimeInSystem { get; } = results.Average(r => r.MeanTimeInSystem);

        public double Processing { get; } = results.Average(r => r.MachineProcessing.Average());

        public ConfidenceInterval Profit { get; } = Interval(results);

        private static ConfidenceInterval Interval(IEnumerable<WorkshopResult> results)
        {
            var sample = new TallyStatistic();
            foreach (var result in results)
                sample.Add(result.ProfitPerTimeUnit);
            return ConfidenceInterval.FromSample(sample);
        }
    }
}
