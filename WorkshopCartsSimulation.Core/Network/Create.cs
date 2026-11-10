using WorkshopCartsSimulation.Core.Distributions;

namespace WorkshopCartsSimulation.Core.Network;

public sealed class Create : Element
{
    private readonly IRandomVariate _delay;
    private readonly Func<long, double, Job> _factory;
    private long _nextId;

    public Create(string name, IRandomVariate delay, Func<long, double, Job> factory) : base(name)
    {
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public long Created => _nextId;

    public long Lost { get; private set; }

    public override void Initialize() => TNext = _delay.Next();

    public override void InAct(Job job) =>
        throw new InvalidOperationException($"{Name} is a source and cannot accept jobs.");

    public override void OutAct()
    {
        var job = _factory(++_nextId, TCurr);
        Quantity++;
        TNext = TCurr + _delay.Next();
        var route = ChooseRoute(job);
        if (route is null)
        {
            Lost++;
            if (Model.Tracing)
                Model.Log($"{Name}: {job} lost");
            return;
        }

        Send(job, route);
    }

    public override void ResetStatistics()
    {
        base.ResetStatistics();
        Lost = 0;
    }
}
