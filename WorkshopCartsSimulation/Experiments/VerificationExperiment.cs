using WorkshopCartsSimulation.Core.Statistics;
using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class VerificationExperiment
{
    private const int AnalyticReplications = 10;

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
            ("Saturation exp(30)", baseline with { MeanInterarrivalTime = 30 }),
            ("One cart of each type", baseline with
            {
                CartsA = CartAssignment.Shared(1, baseline.MachineCount),
                CartsB = CartAssignment.Shared(1, baseline.MachineCount),
            }),
            ("Profit per unit 1000", baseline with { ProfitPerUnit = 1000 }),
        };

        var headers = new[]
        {
            "Scenario", "Lambda", "Throughput", "Processed", "Lq", "Lambda*Wq", "Wq", "W", "Processing", "Cart A", "Cart B",
            "Profit", "Profit/t",
        };

        var rows = scenarios.Select(scenario =>
        {
            var result = new Workshop(scenario.Parameters, ExperimentSettings.Seed).Run(ExperimentSettings.SimulationTime, ExperimentSettings.WarmUpTime);
            var lambda = result.ArrivedUnits / result.ObservationTime;
            return (IReadOnlyList<string>)
            [
                scenario.Name,
                lambda.ToString("F5"),
                (result.ProcessedUnits / result.ObservationTime).ToString("F5"),
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

        Console.WriteLine($"Verification (T = {ExperimentSettings.SimulationTime}, warm-up = {ExperimentSettings.WarmUpTime}, seed = {ExperimentSettings.Seed})");
        Console.WriteLine();
        TablePrinter.Print(headers, rows);
        Console.WriteLine();
        Console.WriteLine($"Minimal time in system (machine 0, no waiting): {MinimalTimeInSystem(baseline):F1}");

        Console.WriteLine();
        Console.WriteLine($"Comparison with analytical values: {AnalyticReplications} runs per scenario, 95% confidence interval");
        PrintAnalyticCheck(baseline);
    }

    private static void PrintAnalyticCheck(WorkshopParameters baseline)
    {
        var saturation = baseline with { MeanInterarrivalTime = 30 };
        var lowLoad = baseline with { MeanInterarrivalTime = 2000, ProcessingTimeStdDev = 0 };
        var faster = baseline with { ProcessingTimeMean = 300 };
        var lambda = 1 / baseline.MeanInterarrivalTime;
        var lowLambda = 1 / lowLoad.MeanInterarrivalTime;
        var fasterLambda = 1 / faster.MeanInterarrivalTime;

        var checks = new (string Scenario, WorkshopParameters Parameters, string Name, double Theory,
            Func<WorkshopResult, double> Selector)[]
        {
            ("Saturation exp(30)", saturation, "Throughput", Capacity(saturation), r => r.ProcessedUnits / r.ObservationTime),
            ("Saturation exp(30)", saturation, "Processing share", SaturatedProcessingShare(saturation), r => r.MachineProcessing.Average()),
            ("Saturation exp(30)", saturation, "Cart A utilization", SaturatedCartUtilization(saturation), r => r.CartAUtilization),
            ("Saturation exp(30)", saturation, "Profit/t", Profit(saturation, Capacity(saturation)), r => r.ProfitPerTimeUnit),
            ("Low load, constant", lowLoad, "Throughput", lowLambda, r => r.ProcessedUnits / r.ObservationTime),
            ("Low load, constant", lowLoad, "Time in system W", MinimalTimeInSystem(lowLoad), r => r.MeanTimeInSystem),
            ("Low load, constant", lowLoad, "Processing share", lowLambda * lowLoad.ProcessingTimeMean / lowLoad.MachineCount, r => r.MachineProcessing.Average()),
            ("Low load, constant", lowLoad, "Cart A utilization",
                lowLambda * (lowLoad.LoadedTravelTime(0) + lowLoad.EmptyTravelTime(0)) / lowLoad.CartsA.CartCount, r => r.CartAUtilization),
            ("Faster norm(300, 80)", faster, "Throughput", fasterLambda, r => r.ProcessedUnits / r.ObservationTime),
            ("Faster norm(300, 80)", faster, "Processing share", fasterLambda * faster.ProcessingTimeMean / faster.MachineCount, r => r.MachineProcessing.Average()),
            ("Faster norm(300, 80)", faster, "Profit/t", Profit(faster, fasterLambda), r => r.ProfitPerTimeUnit),
            ("Baseline exp(99)", baseline, "Processing share", lambda * baseline.ProcessingTimeMean / baseline.MachineCount, r => r.MachineProcessing.Average()),
            ("Baseline exp(99)", baseline, "Profit/t", Profit(baseline, lambda), r => r.ProfitPerTimeUnit),
        };

        var cache = new Dictionary<WorkshopParameters, IReadOnlyList<WorkshopResult>>(ReferenceEqualityComparer.Instance);
        var headers = new[] { "Scenario", "Characteristic", "Theory", "Simulation", "+/-", "Deviation" };
        var rows = checks.Select(check =>
        {
            if (!cache.TryGetValue(check.Parameters, out var results))
            {
                results = Replications.Run(check.Parameters, AnalyticReplications);
                cache[check.Parameters] = results;
            }

            var sample = new TallyStatistic();
            foreach (var result in results)
                sample.Add(check.Selector(result));
            var interval = ConfidenceInterval.FromSample(sample);
            return (IReadOnlyList<string>)
            [
                check.Scenario,
                check.Name,
                check.Theory.ToString("G5"),
                interval.Mean.ToString("G5"),
                interval.HalfWidth.ToString("G3"),
                (Math.Abs(interval.Mean - check.Theory) / Math.Abs(check.Theory)).ToString("P2"),
            ];
        }).ToList();
        TablePrinter.Print(headers, rows);

        var little = Replications.Run(baseline, AnalyticReplications);
        var queue = new TallyStatistic();
        var product = new TallyStatistic();
        foreach (var result in little)
        {
            queue.Add(result.MeanQueueLength);
            product.Add(result.ArrivedUnits / result.ObservationTime * result.MeanWaitingTime);
        }

        Console.WriteLine();
        Console.WriteLine($"Little's law for the queue Q0 (baseline): Lq = {queue.Mean:F2}, lambda * Wq = {product.Mean:F2}");
    }

    private static double MachineCycle(WorkshopParameters parameters, int machine) =>
        parameters.LoadedTravelTime(machine) + parameters.ProcessingTimeMean + parameters.EmptyTravelTime(machine);

    private static double Capacity(WorkshopParameters parameters) =>
        Enumerable.Range(0, parameters.MachineCount).Sum(machine => 1 / MachineCycle(parameters, machine));

    private static double SaturatedProcessingShare(WorkshopParameters parameters) =>
        Enumerable.Range(0, parameters.MachineCount)
            .Average(machine => parameters.ProcessingTimeMean / MachineCycle(parameters, machine));

    private static double SaturatedCartUtilization(WorkshopParameters parameters) =>
        Enumerable.Range(0, parameters.MachineCount)
            .Sum(machine => (parameters.LoadedTravelTime(machine) + parameters.EmptyTravelTime(machine)) /
                            MachineCycle(parameters, machine)) / parameters.CartsA.CartCount;

    private static double Profit(WorkshopParameters parameters, double throughput) =>
        parameters.ProfitPerUnit * throughput -
        (parameters.CartsA.CartCount + parameters.CartsB.CartCount) * parameters.CartCostPerTimeUnit;

    private static double MinimalTimeInSystem(WorkshopParameters parameters) =>
        parameters.LoadedTravelTime(0) + parameters.ProcessingTimeMean + parameters.EmptyTravelTime(0) +
        parameters.LoadedTravelTime(0);
}
