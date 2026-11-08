using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class Replications
{
    public static IReadOnlyList<WorkshopResult> Run(WorkshopParameters parameters, int count) =>
        Enumerable.Range(1, count)
            .AsParallel()
            .AsOrdered()
            .Select(seed => new Workshop(parameters, seed)
                .Run(ExperimentSettings.SimulationTime, ExperimentSettings.WarmUpTime))
            .ToList();
}
