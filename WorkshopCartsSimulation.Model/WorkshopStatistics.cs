using WorkshopCartsSimulation.Core.Statistics;

namespace WorkshopCartsSimulation.Model;

internal sealed class WorkshopStatistics
{
    public WorkshopStatistics(int machineCount)
    {
        MachineOccupied = Enumerable.Range(0, machineCount).Select(_ => new TimeWeightedStatistic()).ToArray();
        MachineProcessing = Enumerable.Range(0, machineCount).Select(_ => new TimeWeightedStatistic()).ToArray();
        MachineBlocked = Enumerable.Range(0, machineCount).Select(_ => new TimeWeightedStatistic()).ToArray();
    }

    public double StartTime { get; private set; }

    public long ArrivedUnits { get; set; }

    public long ProcessedUnits { get; set; }

    public TimeWeightedStatistic QueueLength { get; } = new();

    public TimeWeightedStatistic BusyCartsA { get; } = new();

    public TimeWeightedStatistic BusyCartsB { get; } = new();

    public TimeWeightedStatistic[] MachineOccupied { get; }

    public TimeWeightedStatistic[] MachineProcessing { get; }

    public TimeWeightedStatistic[] MachineBlocked { get; }

    public TallyStatistic WaitingTime { get; } = new();

    public TallyStatistic TimeInSystem { get; } = new();

    public void Observe(double time, int queueLength, IReadOnlyList<MachineState> machines, int busyCartsA, int busyCartsB)
    {
        QueueLength.Update(time, queueLength);
        BusyCartsA.Update(time, busyCartsA);
        BusyCartsB.Update(time, busyCartsB);
        for (var i = 0; i < machines.Count; i++)
        {
            MachineOccupied[i].Update(time, machines[i] == MachineState.Free ? 0 : 1);
            MachineProcessing[i].Update(time, machines[i] == MachineState.Processing ? 1 : 0);
            MachineBlocked[i].Update(time,
                machines[i] is MachineState.AwaitingPickup or MachineState.PickupScheduled ? 1 : 0);
        }
    }

    public void Reset(double time)
    {
        StartTime = time;
        ArrivedUnits = 0;
        ProcessedUnits = 0;
        QueueLength.Reset(time);
        BusyCartsA.Reset(time);
        BusyCartsB.Reset(time);
        foreach (var statistic in MachineOccupied.Concat(MachineProcessing).Concat(MachineBlocked))
            statistic.Reset(time);
        WaitingTime.Reset();
        TimeInSystem.Reset();
    }
}
