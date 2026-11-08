using System.Globalization;
using System.Text;
using WorkshopCartsSimulation.Core.Statistics;
using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class CartCountExperiment
{
    private const int ReplicationCount = 30;
    private const int MaxCarts = 3;
    private const string OutputFile = "carts.csv";

    public static void Run()
    {
        var machines = WorkshopParameters.DefaultMachineCount;
        var points = Enumerable.Range(1, MaxCarts)
            .SelectMany(cartsA => Enumerable.Range(1, MaxCarts).Select(cartsB => (CartsA: cartsA, CartsB: cartsB)))
            .Select(point =>
            {
                var parameters = new WorkshopParameters
                {
                    CartsA = CartAssignment.Shared(point.CartsA, machines),
                    CartsB = CartAssignment.Shared(point.CartsB, machines),
                };
                var results = Replications.Run(parameters, ReplicationCount);
                var profit = new TallyStatistic();
                foreach (var result in results)
                    profit.Add(result.ProfitPerTimeUnit);

                return new CartPoint(
                    point.CartsA,
                    point.CartsB,
                    results.Average(r => r.ProcessedUnits / r.ObservationTime),
                    results.Average(r => r.MeanTimeInSystem),
                    results.Average(r => r.CartAUtilization),
                    results.Average(r => r.CartBUtilization),
                    results.Average(r => r.MachineBlocked.Average()),
                    ConfidenceInterval.FromSample(profit));
            })
            .ToList();

        Console.WriteLine($"Proposal: number of shared carts, {ReplicationCount} runs per point, " +
                          $"T = {ExperimentSettings.SimulationTime}, warm-up = {ExperimentSettings.WarmUpTime}");
        Console.WriteLine();

        var headers = new[] { "Carts A", "Carts B", "Throughput", "W", "Cart A", "Cart B", "Blocked", "Profit/t", "+/-" };
        var rows = points.Select(point => (IReadOnlyList<string>)
        [
            point.CartsA.ToString(),
            point.CartsB.ToString(),
            point.Throughput.ToString("F5"),
            point.TimeInSystem.ToString("F0"),
            point.CartAUtilization.ToString("F3"),
            point.CartBUtilization.ToString("F3"),
            point.Blocked.ToString("F4"),
            point.Profit.Mean.ToString("F4"),
            point.Profit.HalfWidth.ToString("F4"),
        ]).ToList();
        TablePrinter.Print(headers, rows);

        var best = points.MaxBy(point => point.Profit.Mean)!;
        Console.WriteLine();
        Console.WriteLine($"Best: {best.CartsA} cart(s) A and {best.CartsB} cart(s) B, " +
                          $"profit {best.Profit.Mean:F4} +/- {best.Profit.HalfWidth:F4} per time unit");

        WriteCsv(points);
        Console.WriteLine($"Results saved to {Path.GetFullPath(OutputFile)}");
    }

    private static void WriteCsv(IEnumerable<CartPoint> points)
    {
        var builder = new StringBuilder("carts_a;carts_b;throughput;time_in_system;cart_a;cart_b;blocked;profit_rate;profit_half_width");
        builder.AppendLine();
        foreach (var p in points)
        {
            var values = new[]
            {
                p.Throughput, p.TimeInSystem, p.CartAUtilization, p.CartBUtilization, p.Blocked, p.Profit.Mean, p.Profit.HalfWidth,
            };
            builder.AppendLine($"{p.CartsA};{p.CartsB};" +
                               string.Join(";", values.Select(v => v.ToString("G6", CultureInfo.InvariantCulture))));
        }

        File.WriteAllText(OutputFile, builder.ToString());
    }

    private sealed record CartPoint(
        int CartsA,
        int CartsB,
        double Throughput,
        double TimeInSystem,
        double CartAUtilization,
        double CartBUtilization,
        double Blocked,
        ConfidenceInterval Profit);
}
