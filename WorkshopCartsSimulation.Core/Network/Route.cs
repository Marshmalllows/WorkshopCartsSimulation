namespace WorkshopCartsSimulation.Core.Network;

public sealed class Route
{
    public Route(Element target, int priority, Func<Job, bool>? block, string? jobType)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Priority = priority;
        Block = block;
        JobType = jobType;
    }

    public Element Target { get; }

    public int Priority { get; }

    public Func<Job, bool>? Block { get; }

    public string? JobType { get; }

    public long Used { get; internal set; }

    public bool Accepts(Job job) => JobType is null || JobType == job.Type;

    public bool IsBlocked(Job job) => Block is not null && Block(job);
}
