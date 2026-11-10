namespace WorkshopCartsSimulation.Core.Network;

public sealed class Model
{
    private readonly List<Element> _elements = [];
    private readonly List<(Element Element, int Channel)> _blocked = [];

    public double TCurr { get; private set; }

    public double StatisticsStart { get; private set; }

    public double ObservationTime => TCurr - StatisticsStart;

    public long ProcessedEvents { get; private set; }

    public IReadOnlyList<Element> Elements => _elements;

    public Action<Element, Job, Route>? RouteChosen { get; set; }

    public Action<double>? InstantProcessed { get; set; }

    public Action<double>? StatisticsReset { get; set; }

    public Action<double, string>? Trace { get; set; }

    public T Add<T>(T element) where T : Element
    {
        ArgumentNullException.ThrowIfNull(element);
        element.Id = _elements.Count;
        element.Model = this;
        _elements.Add(element);
        return element;
    }

    public void Simulate(double endTime, double warmUpTime = 0, double sampleInterval = 0,
        Action<double>? sample = null)
    {
        if (warmUpTime < 0 || warmUpTime >= endTime)
            throw new ArgumentOutOfRangeException(nameof(warmUpTime), "Warm-up time must be in [0, endTime).");

        foreach (var element in _elements)
            element.Initialize();

        var warmUpPending = warmUpTime > 0;
        var nextSample = sampleInterval > 0 ? sampleInterval : double.PositiveInfinity;
        while (true)
        {
            var tnext = NextEventTime();
            var horizon = Math.Min(tnext, endTime);
            if (warmUpPending && warmUpTime <= horizon)
            {
                Advance(warmUpTime);
                ResetStatistics();
                warmUpPending = false;
            }

            while (nextSample <= horizon)
            {
                Advance(nextSample);
                sample?.Invoke(nextSample);
                nextSample += sampleInterval;
            }

            if (tnext > endTime)
            {
                Advance(endTime);
                return;
            }

            Advance(tnext);
            ProcessInstant();
        }
    }

    internal void Block(Element element, int channel) => _blocked.Add((element, channel));

    internal void OnRouteChosen(Element from, Job job, Route route) => RouteChosen?.Invoke(from, job, route);

    internal bool Tracing => Trace is not null;

    internal void Log(string message) => Trace?.Invoke(TCurr, message);

    private void ProcessInstant()
    {
        bool progress;
        do
        {
            foreach (var element in _elements)
            {
                if (element.TNext != TCurr)
                    continue;
                element.OutAct();
                ProcessedEvents++;
            }

            progress = ReleaseBlocked();
        } while (progress || NextEventTime() == TCurr);

        InstantProcessed?.Invoke(TCurr);
    }

    private double NextEventTime()
    {
        var tnext = double.PositiveInfinity;
        foreach (var element in _elements)
            tnext = Math.Min(tnext, element.TNext);
        return tnext;
    }

    private bool ReleaseBlocked()
    {
        var any = false;
        var progress = true;
        while (progress)
        {
            progress = false;
            for (var i = 0; i < _blocked.Count; i++)
            {
                var (element, channel) = _blocked[i];
                if (!element.TryRelease(channel))
                    continue;
                _blocked.RemoveAt(i);
                progress = true;
                any = true;
                break;
            }
        }

        return any;
    }

    private void Advance(double time)
    {
        var delta = time - TCurr;
        if (delta < 0)
            throw new InvalidOperationException("Time cannot go backwards.");
        foreach (var element in _elements)
            element.DoStatistics(delta);
        TCurr = time;
    }

    private void ResetStatistics()
    {
        StatisticsStart = TCurr;
        foreach (var element in _elements)
            element.ResetStatistics();
        StatisticsReset?.Invoke(TCurr);
    }
}
