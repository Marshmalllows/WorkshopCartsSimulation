using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal sealed record AssignmentVariant(string Code, string Description, CartAssignment CartsA, CartAssignment CartsB)
{
    public string Name => $"{Code} {Description}";

    public WorkshopParameters Parameters(WorkshopParameters? baseParameters = null) =>
        (baseParameters ?? new WorkshopParameters()) with { CartsA = CartsA, CartsB = CartsB };
}
