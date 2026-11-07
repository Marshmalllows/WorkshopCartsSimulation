namespace WorkshopCartsSimulation.Core.Distributions;

public interface IRandomVariate
{
    double Next();

    double Mean { get; }
}
