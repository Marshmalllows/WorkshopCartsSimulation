namespace WorkshopCartsSimulation.Model;

public sealed record WorkshopSnapshot(
    double Time,
    int WaitingUnits,
    IReadOnlyList<MachineState> Machines,
    IReadOnlyList<bool> CartsABusy,
    IReadOnlyList<bool> CartsBBusy,
    IReadOnlyList<int> PickupQueue);
