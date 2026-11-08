using WorkshopCartsSimulation.Model;

const double simulationTime = 100_000;
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
    var result = new Workshop(parameters, seed).Run(simulationTime);
    Console.WriteLine($"{name}: A {parameters.CartsA}, B {parameters.CartsB}");
    Console.WriteLine($"  Arrived units:   {result.ArrivedUnits}");
    Console.WriteLine($"  Processed units: {result.ProcessedUnits}");
    Console.WriteLine($"  Units in system: {result.UnitsInSystem}");
    Console.WriteLine($"  Revenue:         {result.Revenue:F0}");
    Console.WriteLine($"  Cart cost:       {result.CartCost:F0}");
    Console.WriteLine($"  Profit:          {result.Profit:F0}");
    Console.WriteLine();
}
