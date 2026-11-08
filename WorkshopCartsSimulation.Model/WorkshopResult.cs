namespace WorkshopCartsSimulation.Model;

public sealed record WorkshopResult(
    double SimulationTime,
    long ArrivedUnits,
    long ProcessedUnits,
    long UnitsInSystem,
    double Revenue,
    double CartCost)
{
    public double Profit => Revenue - CartCost;
}
