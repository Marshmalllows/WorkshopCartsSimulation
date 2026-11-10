using WorkshopCartsSimulation.Core.Statistics;

namespace WorkshopCartsSimulation.Core.Network;

public sealed class Dispose : Element
{
    public Dispose(string name) : base(name)
    {
    }

    public long Total { get; private set; }

    public TallyStatistic TimeInSystem { get; } = new();

    public override void InAct(Job job)
    {
        Quantity++;
        Total++;
        TimeInSystem.Add(TCurr - job.CreatedTime);
    }

    public override void ResetStatistics()
    {
        base.ResetStatistics();
        TimeInSystem.Reset();
    }
}
