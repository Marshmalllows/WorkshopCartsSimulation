using System.Globalization;
using System.Text;
using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class WarmUpExperiment
{
    private const double Horizon = 400_000;
    private const double SampleInterval = 2_000;
    private const int Replications = 20;
    private const int MovingAverageWindow = 10;
    private const string OutputFile = "warmup.csv";

    public static void Run()
    {
        var parameters = new WorkshopParameters();
        var runs = Enumerable.Range(1, Replications)
            .Select(seed => new Workshop(parameters, seed).RunWithSampling(Horizon, SampleInterval))
            .ToList();

        var pointCount = runs.Min(run => run.Count);
        var times = Enumerable.Range(0, pointCount).Select(i => runs[0][i].Time).ToArray();
        var queue = Average(runs, pointCount, sample => sample.MeanQueueLength);
        var timeInSystem = Average(runs, pointCount, sample => sample.MeanTimeInSystem);
        var profit = Average(runs, pointCount, sample => sample.ProfitPerTimeUnit);
        var queueSmoothed = MovingAverage(queue, MovingAverageWindow);
        var timeInSystemSmoothed = MovingAverage(timeInSystem, MovingAverageWindow);
        var profitSmoothed = MovingAverage(profit, MovingAverageWindow);

        WriteCsv(times, queue, queueSmoothed, timeInSystem, timeInSystemSmoothed, profit, profitSmoothed);

        Console.WriteLine($"Warm-up analysis (Welch method): {Replications} replications, horizon {Horizon}, " +
                          $"interval {SampleInterval}, moving average window {MovingAverageWindow}");
        Console.WriteLine();

        var headers = new[] { "Time", "Queue", "Queue MA", "Time in system", "TIS MA", "Profit/t", "Profit/t MA" };
        var rows = Enumerable.Range(0, pointCount)
            .Where(i => times[i] <= 20_000 || (i + 1) % 10 == 0)
            .Select(i => (IReadOnlyList<string>)
            [
                times[i].ToString("F0"),
                queue[i].ToString("F2"),
                queueSmoothed[i].ToString("F2"),
                timeInSystem[i].ToString("F1"),
                timeInSystemSmoothed[i].ToString("F1"),
                profit[i].ToString("F3"),
                profitSmoothed[i].ToString("F3"),
            ])
            .ToList();
        TablePrinter.Print(headers, rows);

        Console.WriteLine();
        Console.WriteLine($"Full series saved to {Path.GetFullPath(OutputFile)}");
    }

    private static double[] Average(IReadOnlyList<IReadOnlyList<WorkshopSample>> runs, int pointCount,
        Func<WorkshopSample, double> selector) =>
        Enumerable.Range(0, pointCount)
            .Select(i => runs.Select(run => selector(run[i])).Where(value => !double.IsNaN(value)).DefaultIfEmpty(double.NaN).Average())
            .ToArray();

    private static double[] MovingAverage(IReadOnlyList<double> values, int window) =>
        Enumerable.Range(0, values.Count)
            .Select(i =>
            {
                var halfWidth = Math.Min(window, Math.Min(i, values.Count - 1 - i));
                return Enumerable.Range(i - halfWidth, 2 * halfWidth + 1).Average(j => values[j]);
            })
            .ToArray();

    private static void WriteCsv(IReadOnlyList<double> times, params IReadOnlyList<double>[] series)
    {
        var builder = new StringBuilder("time;queue;queue_ma;time_in_system;time_in_system_ma;profit_rate;profit_rate_ma");
        builder.AppendLine();
        for (var i = 0; i < times.Count; i++)
        {
            var values = new[] { times[i] }.Concat(series.Select(s => s[i]));
            builder.AppendLine(string.Join(";", values.Select(v => v.ToString("G6", CultureInfo.InvariantCulture))));
        }

        File.WriteAllText(OutputFile, builder.ToString());
    }
}
