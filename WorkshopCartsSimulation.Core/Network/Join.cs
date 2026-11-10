namespace WorkshopCartsSimulation.Core.Network;

public sealed class Join : Element
{
    private readonly List<Job> _buffer = [];
    private long _nextId;

    public Join(string name, int size) : base(name)
    {
        Size = size >= 2 ? size : throw new ArgumentOutOfRangeException(nameof(size), "Group size must be at least 2.");
    }

    public int Size { get; }

    public int Count => _buffer.Count;

    public IReadOnlyList<Job> Buffer => _buffer;

    public override void InAct(Job job)
    {
        _buffer.Add(job);
        if (_buffer.Count < Size)
            return;

        var members = _buffer.ToArray();
        _buffer.Clear();
        var group = new Job(++_nextId, "group", members.Min(member => member.CreatedTime), members);
        Quantity++;
        var route = ChooseRoute(group)
                    ?? throw new InvalidOperationException($"{Name} has no open route for {group}.");
        Send(group, route);
    }
}
