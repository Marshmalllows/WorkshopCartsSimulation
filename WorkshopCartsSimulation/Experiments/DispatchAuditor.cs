using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal sealed class DispatchAuditor
{
    public const string DeliveryNeedsUnit = "Cart A leaves only when a unit waits";
    public const string NearestMachine = "Cart A goes to the nearest free reachable machine";
    public const string LowestCartA = "Lowest-index free eligible cart A is used";
    public const string PickupOrder = "Cart B serves pickups in FIFO order";
    public const string LowestCartB = "Lowest-index free eligible cart B is used";
    public const string NoIdleDelivery = "No waiting unit while delivery is possible";
    public const string NoIdlePickup = "No finished unit while pickup is possible";
    public const string UnitBalance = "Arrived = processed + in system";

    public static readonly string[] Checks =
    [
        DeliveryNeedsUnit, NearestMachine, LowestCartA, PickupOrder, LowestCartB, NoIdleDelivery, NoIdlePickup,
        UnitBalance,
    ];

    private readonly WorkshopParameters _parameters;

    public DispatchAuditor(WorkshopParameters parameters)
    {
        _parameters = parameters;
        Deliveries = new long[parameters.MachineCount];
        Pickups = new long[parameters.MachineCount];
    }

    public Dictionary<string, long> Checked { get; } = Checks.ToDictionary(check => check, _ => 0L);

    public Dictionary<string, long> Violations { get; } = Checks.ToDictionary(check => check, _ => 0L);

    public long[] Deliveries { get; }

    public long[] Pickups { get; }

    public void Attach(Workshop workshop)
    {
        workshop.CartDispatched = OnDispatch;
        workshop.EventCompleted = OnEventCompleted;
    }

    public void CheckBalance(WorkshopResult result) =>
        Record(UnitBalance, result.ArrivedUnits == result.ProcessedUnits + result.UnitsInSystemAtEnd);

    private void OnDispatch(CartDispatch dispatch)
    {
        if (dispatch.Type == CartType.A)
            CheckDelivery(dispatch);
        else
            CheckPickup(dispatch);
    }

    private void CheckDelivery(CartDispatch dispatch)
    {
        var state = dispatch.Before;
        Deliveries[dispatch.Machine]++;
        Record(DeliveryNeedsUnit, state.WaitingUnits > 0 && !state.CartsABusy[dispatch.Cart]);

        var expectedMachine = -1;
        for (var machine = 0; machine < _parameters.MachineCount && expectedMachine < 0; machine++)
        {
            if (state.Machines[machine] == MachineState.Free && FirstFreeCart(state.CartsABusy, _parameters.CartsA, machine) >= 0)
                expectedMachine = machine;
        }

        Record(NearestMachine, dispatch.Machine == expectedMachine);
        Record(LowestCartA, dispatch.Cart == FirstFreeCart(state.CartsABusy, _parameters.CartsA, dispatch.Machine));
    }

    private void CheckPickup(CartDispatch dispatch)
    {
        var state = dispatch.Before;
        Pickups[dispatch.Machine]++;

        var expectedMachine = state.PickupQueue
            .Where(machine => FirstFreeCart(state.CartsBBusy, _parameters.CartsB, machine) >= 0)
            .DefaultIfEmpty(-1)
            .First();
        Record(PickupOrder, dispatch.Machine == expectedMachine &&
                            state.Machines[dispatch.Machine] == MachineState.AwaitingPickup);
        Record(LowestCartB, dispatch.Cart == FirstFreeCart(state.CartsBBusy, _parameters.CartsB, dispatch.Machine));
    }

    private void OnEventCompleted(WorkshopSnapshot state)
    {
        var deliveryPossible = state.WaitingUnits > 0 && Enumerable.Range(0, _parameters.MachineCount).Any(machine =>
            state.Machines[machine] == MachineState.Free && FirstFreeCart(state.CartsABusy, _parameters.CartsA, machine) >= 0);
        Record(NoIdleDelivery, !deliveryPossible);

        var pickupPossible = state.PickupQueue.Any(machine =>
            FirstFreeCart(state.CartsBBusy, _parameters.CartsB, machine) >= 0);
        Record(NoIdlePickup, !pickupPossible);
    }

    private void Record(string check, bool passed)
    {
        Checked[check]++;
        if (!passed)
            Violations[check]++;
    }

    private static int FirstFreeCart(IReadOnlyList<bool> busy, CartAssignment assignment, int machine)
    {
        for (var cart = 0; cart < busy.Count; cart++)
        {
            if (!busy[cart] && assignment.CanServe(cart, machine))
                return cart;
        }

        return -1;
    }
}
