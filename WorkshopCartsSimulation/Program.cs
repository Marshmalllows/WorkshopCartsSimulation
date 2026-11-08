using System.Globalization;
using WorkshopCartsSimulation.Model;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

const double simulationTime = 100_000;
const double warmUpTime = 10_000;
const int seed = 1;

var variants = new (string Name, WorkshopParameters Parameters)[]
{
    ("Shared carts", new WorkshopParameters()),
    ("Dedicated carts", new WorkshopParameters
    {
        CartsA = CartAssignment.Dedicated(5, [0, 1], [2, 3], [4]),
        CartsB = CartAssignment.Dedicated(5, [0, 1], [2, 3], [4]),
    }),
};

foreach (var (name, parameters) in variants)
{
    var result = new Workshop(parameters, seed).Run(simulationTime, warmUpTime);
    PrintResult(name, parameters, result);
}

static void PrintResult(string name, WorkshopParameters parameters, WorkshopResult result)
{
    Console.WriteLine($"{name}: A {parameters.CartsA}, B {parameters.CartsB}");
    Console.WriteLine($"  Observation time:         {result.ObservationTime:F0}");
    Console.WriteLine($"  Arrived / processed:      {result.ArrivedUnits} / {result.ProcessedUnits}");
    Console.WriteLine($"  Units in system at end:   {result.UnitsInSystemAtEnd}");
    Console.WriteLine($"  Mean / max queue length:  {result.MeanQueueLength:F2} / {result.MaxQueueLength:F0}");
    Console.WriteLine($"  Mean waiting time:        {result.MeanWaitingTime:F1}");
    Console.WriteLine($"  Mean time in system:      {result.MeanTimeInSystem:F1}");
    Console.WriteLine($"  Machine occupancy:        {FormatShares(result.MachineOccupancy)}");
    Console.WriteLine($"  Machine processing share: {FormatShares(result.MachineProcessing)}");
    Console.WriteLine($"  Cart A / B utilization:   {result.CartAUtilization:F3} / {result.CartBUtilization:F3}");
    Console.WriteLine($"  Revenue / cart cost:      {result.Revenue:F0} / {result.CartCost:F0}");
    Console.WriteLine($"  Profit:                   {result.Profit:F0} ({result.ProfitPerTimeUnit:F3} per time unit)");
    Console.WriteLine();
}

static string FormatShares(IEnumerable<double> shares) => string.Join("  ", shares.Select(s => s.ToString("F3")));
