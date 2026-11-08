namespace WorkshopCartsSimulation.Model;

public sealed record WorkshopResult
{
    public required double ObservationTime { get; init; }

    public required long ArrivedUnits { get; init; }

    public required long ProcessedUnits { get; init; }

    public required long UnitsInSystemAtEnd { get; init; }

    public required double MeanQueueLength { get; init; }

    public required double MaxQueueLength { get; init; }

    public required double MeanWaitingTime { get; init; }

    public required double MeanTimeInSystem { get; init; }

    public required IReadOnlyList<double> MachineOccupancy { get; init; }

    public required IReadOnlyList<double> MachineProcessing { get; init; }

    public required double CartAUtilization { get; init; }

    public required double CartBUtilization { get; init; }

    public required double Revenue { get; init; }

    public required double CartCost { get; init; }

    public double Profit => Revenue - CartCost;

    public double ProfitPerTimeUnit => Profit / ObservationTime;
}
