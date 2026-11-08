namespace WorkshopCartsSimulation.Model;

public sealed record WorkshopParameters
{
    public const int DefaultMachineCount = 5;

    public double MeanInterarrivalTime { get; init; } = 99;

    public double ProcessingTimeMean { get; init; } = 450;

    public double ProcessingTimeStdDev { get; init; } = 80;

    public int MachineCount { get; init; } = DefaultMachineCount;

    public double ProfitPerUnit { get; init; } = 700;

    public double CartCostPerTimeUnit { get; init; } = 1;

    public CartAssignment CartsA { get; init; } = CartAssignment.Shared(3, DefaultMachineCount);

    public CartAssignment CartsB { get; init; } = CartAssignment.Shared(3, DefaultMachineCount);

    public double LoadedTravelTime(int machine) => 15 + 2 * (machine + 1);

    public double EmptyTravelTime(int machine) => 15 + 3 * (machine + 1);
}
