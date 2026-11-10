namespace WorkshopCartsSimulation.Core.Network;

public sealed class Job
{
    public Job(long id, string type, double createdTime, IReadOnlyList<Job>? members = null)
    {
        Id = id;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        CreatedTime = createdTime;
        Members = members ?? [];
    }

    public long Id { get; }

    public string Type { get; }

    public double CreatedTime { get; }

    public double ServiceTime { get; set; }

    public IReadOnlyList<Job> Members { get; }

    public override string ToString() =>
        Members.Count == 0 ? $"{Type} {Id}" : "[" + string.Join(" + ", Members) + "]";
}
