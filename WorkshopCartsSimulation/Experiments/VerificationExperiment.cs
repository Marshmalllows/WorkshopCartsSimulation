using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class VerificationExperiment
{
    private const double SimulationTime = 100_000;
    private const double WarmUpTime = 10_000;
    private const int Seed = 1;

    public static void Run()
    {
        var baseline = new WorkshopParameters();
        var scenarios = new (string Name, WorkshopParameters Parameters)[]
        {
            ("Baseline", baseline),
            ("Low load exp(2000)", baseline with { MeanInterarrivalTime = 2000 }),
            ("Low load, constant processing", baseline with { MeanInterarrivalTime = 2000, ProcessingTimeStdDev = 0 }),
            ("Faster processing norm(300, 80)", baseline with { ProcessingTimeMean = 300 }),
            ("Higher load exp(90)", baseline with { MeanInterarrivalTime = 90 }),
            ("One cart of each type", baseline with
            {
                CartsA = CartAssignment.Shared(1, baseline.MachineCount),
                CartsB = CartAssignment.Shared(1, baseline.MachineCount),
            }),
            ("Profit per unit 1000", baseline with { ProfitPerUnit = 1000 }),
        };

        var headers = new[]
        {
            "Scenario", "Lambda", "Processed", "Lq", "Lambda*Wq", "Wq", "W", "Processing", "Cart A", "Cart B",
            "Profit", "Profit/t",
        };

        var rows = scenarios.Select(scenario =>
        {
            var result = new Workshop(scenario.Parameters, Seed).Run(SimulationTime, WarmUpTime);
            var lambda = result.ArrivedUnits / result.ObservationTime;
            return (IReadOnlyList<string>)
            [
                scenario.Name,
                lambda.ToString("F5"),
                result.ProcessedUnits.ToString(),
                result.MeanQueueLength.ToString("F2"),
                (lambda * result.MeanWaitingTime).ToString("F2"),
                result.MeanWaitingTime.ToString("F1"),
                result.MeanTimeInSystem.ToString("F1"),
                result.MachineProcessing.Average().ToString("F3"),
                result.CartAUtilization.ToString("F3"),
                result.CartBUtilization.ToString("F3"),
                result.Profit.ToString("F0"),
                result.ProfitPerTimeUnit.ToString("F3"),
            ];
        }).ToList();

        Console.WriteLine($"Verification (T = {SimulationTime}, warm-up = {WarmUpTime}, seed = {Seed})");
        Console.WriteLine();
        TablePrinter.Print(headers, rows);
        Console.WriteLine();
        Console.WriteLine($"Minimal time in system (machine 0, no waiting): {MinimalTimeInSystem(baseline):F1}");
    }

    private static double MinimalTimeInSystem(WorkshopParameters parameters) =>
        parameters.LoadedTravelTime(0) + parameters.ProcessingTimeMean + parameters.EmptyTravelTime(0) +
        parameters.LoadedTravelTime(0);
}
