namespace WorkshopCartsSimulation.Core.Network;

public abstract class Element
{
    private readonly List<Route> _routes = [];

    protected Element(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name;
    }

    public string Name { get; }

    public int Id { get; internal set; }

    public Model Model { get; internal set; } = null!;

    public double TNext { get; protected set; } = double.PositiveInfinity;

    public double TCurr => Model.TCurr;

    public long Quantity { get; protected set; }

    public IReadOnlyList<Route> Routes => _routes;

    public Route AddRoute(Element target, int priority = 0, Func<Job, bool>? block = null, string? jobType = null)
    {
        var route = new Route(target, priority, block, jobType);
        _routes.Add(route);
        return route;
    }

    public abstract void InAct(Job job);

    public virtual void OutAct()
    {
    }

    public virtual void Initialize()
    {
    }

    public virtual void DoStatistics(double delta)
    {
    }

    public virtual void ResetStatistics()
    {
        Quantity = 0;
    }

    public virtual bool TryRelease(int channel) => false;

    public Route? ChooseRoute(Job job)
    {
        Route? best = null;
        foreach (var route in _routes)
        {
            if (!route.Accepts(job) || route.IsBlocked(job))
                continue;
            if (best is null || route.Priority > best.Priority)
                best = route;
        }

        return best;
    }

    protected void Send(Job job, Route route)
    {
        Notify(job, route);
        Deliver(job, route);
    }

    protected void Notify(Job job, Route route) => Model.OnRouteChosen(this, job, route);

    protected void Deliver(Job job, Route route)
    {
        route.Used++;
        if (Model.Tracing)
            Model.Log($"{Name} -> {route.Target.Name}: {job}");
        route.Target.InAct(job);
    }
}
