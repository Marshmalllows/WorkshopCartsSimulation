using System.Globalization;
using System.Text;
using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class RunLengthExperiment
{
    private const int RunCount = 6;
    private const double Horizon = 1_000_000;
    private const double SampleInterval = 5_000;
    private const double Tolerance = 0.2;
    private const string OutputFile = "runlength.csv";

    public static void Run()
    {
        var parameters = new WorkshopParameters();
        var runs = Enumerable.Range(1, RunCount)
            .AsParallel()
            .AsOrdered()
            .Select(seed => CumulativeProfit(new Workshop(parameters, seed).RunWithSampling(Horizon, SampleInterval)))
            .ToList();

        var pointCount = runs.Min(run => run.Length);
        var times = Enumerable.Range(1, pointCount).Select(i => i * SampleInterval).ToArray();
        var spreads = Enumerable.Range(0, pointCount)
            .Select(i => runs.Max(run => run[i]) - runs.Min(run => run[i]))
            .ToArray();
        var lastViolation = Array.FindLastIndex(spreads, spread => spread > Tolerance);
        var runLength = lastViolation + 1 < pointCount ? times[lastViolation + 1] : double.NaN;

        Console.WriteLine($"Run length (criterion max|y_i(T) - y_j(T)| <= {Tolerance}): {RunCount} runs, " +
                          $"cumulative mean profit per time unit on [0; t], step {SampleInterval}");
        Console.WriteLine();

        var headers = new[] { "Time" }
            .Concat(Enumerable.Range(1, RunCount).Select(run => $"Run {run}"))
            .Append("Spread")
            .ToArray();
        var rows = Enumerable.Range(0, pointCount)
            .Where(i => times[i] <= 50_000 || times[i] % 100_000 == 0 || times[i] == runLength)
            .Select(i => (IReadOnlyList<string>)new[] { times[i].ToString("F0") }
                .Concat(runs.Select(run => run[i].ToString("F4")))
                .Append(spreads[i].ToString("F4"))
                .ToList())
            .ToList();
        TablePrinter.Print(headers, rows);

        Console.WriteLine();
        Console.WriteLine(double.IsNaN(runLength)
            ? $"Criterion is not satisfied within {Horizon}"
            : $"Criterion holds from T = {runLength:F0}; final spread = {spreads[^1]:F4}");

        WriteCsv(times, runs, spreads);
        Console.WriteLine($"Full series saved to {Path.GetFullPath(OutputFile)}");
    }

    private static double[] CumulativeProfit(IReadOnlyList<WorkshopSample> samples)
    {
        var result = new double[samples.Count];
        var profit = 0.0;
        var previousTime = 0.0;
        for (var i = 0; i < samples.Count; i++)
        {
            profit += samples[i].ProfitPerTimeUnit * (samples[i].Time - previousTime);
            previousTime = samples[i].Time;
            result[i] = profit / samples[i].Time;
        }

        return result;
    }

    private static void WriteCsv(IReadOnlyList<double> times, IReadOnlyList<double[]> runs, IReadOnlyList<double> spreads)
    {
        var builder = new StringBuilder("time;" + string.Join(";", Enumerable.Range(1, runs.Count).Select(run => $"run{run}")) +
                                        ";spread");
        builder.AppendLine();
        for (var i = 0; i < times.Count; i++)
        {
            var values = new[] { times[i] }.Concat(runs.Select(run => run[i])).Append(spreads[i]);
            builder.AppendLine(string.Join(";", values.Select(v => v.ToString("G6", CultureInfo.InvariantCulture))));
        }

        File.WriteAllText(OutputFile, builder.ToString());
    }
}
