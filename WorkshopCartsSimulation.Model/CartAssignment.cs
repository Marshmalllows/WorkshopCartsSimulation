namespace WorkshopCartsSimulation.Model;

public sealed class CartAssignment
{
    private readonly bool[][] _canServe;

    public CartAssignment(IReadOnlyList<IReadOnlyCollection<int>> machinesByCart, int machineCount)
    {
        ArgumentNullException.ThrowIfNull(machinesByCart);
        if (machinesByCart.Count == 0)
            throw new ArgumentException("At least one cart is required.", nameof(machinesByCart));
        if (machineCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(machineCount), "Machine count must be positive.");

        MachineCount = machineCount;
        _canServe = new bool[machinesByCart.Count][];
        for (var cart = 0; cart < machinesByCart.Count; cart++)
        {
            _canServe[cart] = new bool[machineCount];
            foreach (var machine in machinesByCart[cart])
            {
                if (machine < 0 || machine >= machineCount)
                    throw new ArgumentOutOfRangeException(nameof(machinesByCart), $"Machine {machine} does not exist.");
                _canServe[cart][machine] = true;
            }
        }

        for (var machine = 0; machine < machineCount; machine++)
        {
            var covered = false;
            for (var cart = 0; cart < CartCount; cart++)
                covered |= _canServe[cart][machine];
            if (!covered)
                throw new ArgumentException($"Machine {machine} is not served by any cart.", nameof(machinesByCart));
        }
    }

    public int CartCount => _canServe.Length;

    public int MachineCount { get; }

    public bool CanServe(int cart, int machine) => _canServe[cart][machine];

    public static CartAssignment Shared(int cartCount, int machineCount)
    {
        var all = Enumerable.Range(0, machineCount).ToArray();
        var machinesByCart = Enumerable.Repeat<IReadOnlyCollection<int>>(all, cartCount).ToArray();
        return new CartAssignment(machinesByCart, machineCount);
    }

    public static CartAssignment Dedicated(int machineCount, params int[][] machinesByCart) =>
        new(machinesByCart, machineCount);

    public override string ToString()
    {
        var groups = _canServe.Select(row =>
            "{" + string.Join(",", Enumerable.Range(0, MachineCount).Where(m => row[m])) + "}");
        return string.Join(" ", groups);
    }
}
