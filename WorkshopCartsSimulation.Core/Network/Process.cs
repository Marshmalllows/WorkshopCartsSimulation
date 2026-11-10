namespace WorkshopCartsSimulation.Core.Network;

public sealed class Process : Element
{
    private readonly Func<Job, double> _delay;
    private readonly Queue<Job> _queue = new();
    private readonly ChannelState[] _states;
    private readonly Job?[] _jobs;
    private readonly double[] _tnext;
    private readonly double[] _blockedSince;

    public Process(string name, int channels, Func<Job, double> delay, int maxQueue = int.MaxValue) : base(name)
    {
        if (channels <= 0)
            throw new ArgumentOutOfRangeException(nameof(channels), "Channel count must be positive.");
        if (maxQueue < 0)
            throw new ArgumentOutOfRangeException(nameof(maxQueue), "Queue capacity cannot be negative.");

        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        MaxQueue = maxQueue;
        _states = new ChannelState[channels];
        _jobs = new Job?[channels];
        _tnext = Enumerable.Repeat(double.PositiveInfinity, channels).ToArray();
        _blockedSince = new double[channels];
    }

    public int Channels => _states.Length;

    public int MaxQueue { get; }

    public int QueueLength => _queue.Count;

    public int Busy { get; private set; }

    public int Blocked { get; private set; }

    public int Occupied => Busy + Blocked;

    public long Failures { get; private set; }

    public double QueueArea { get; private set; }

    public double BusyArea { get; private set; }

    public double BlockedArea { get; private set; }

    public double MeanQueue => QueueArea / Model.ObservationTime;

    public double MeanBusy => BusyArea / Model.ObservationTime;

    public double MeanBlocked => BlockedArea / Model.ObservationTime;

    public ChannelState State(int channel) => _states[channel];

    public Job? JobAt(int channel) => _jobs[channel];

    public double BlockedSince(int channel) => _blockedSince[channel];

    public IEnumerable<Job> Jobs => _jobs.Where(job => job is not null).Select(job => job!).Concat(_queue);

    public override void InAct(Job job)
    {
        var channel = Array.IndexOf(_states, ChannelState.Free);
        if (channel >= 0)
        {
            Start(channel, job);
            return;
        }

        if (_queue.Count < MaxQueue)
        {
            _queue.Enqueue(job);
            return;
        }

        Failures++;
        if (Model.Tracing)
            Model.Log($"{Name}: {job} rejected");
    }

    public override void OutAct()
    {
        for (var channel = 0; channel < Channels; channel++)
        {
            if (_states[channel] != ChannelState.Busy || _tnext[channel] > TCurr)
                continue;

            var job = _jobs[channel]!;
            Quantity++;
            var route = ChooseRoute(job);
            if (route is null)
            {
                _states[channel] = ChannelState.Blocked;
                Busy--;
                Blocked++;
                _tnext[channel] = double.PositiveInfinity;
                _blockedSince[channel] = TCurr;
                Model.Block(this, channel);
                if (Model.Tracing)
                    Model.Log($"{Name}: {job} blocked");
            }
            else
            {
                Notify(job, route);
                Release(channel);
                Deliver(job, route);
            }
        }

        UpdateTNext();
    }

    public override bool TryRelease(int channel)
    {
        var job = _jobs[channel]!;
        var route = ChooseRoute(job);
        if (route is null)
            return false;

        Notify(job, route);
        Release(channel);
        Deliver(job, route);
        return true;
    }

    public override void DoStatistics(double delta)
    {
        QueueArea += _queue.Count * delta;
        BusyArea += Busy * delta;
        BlockedArea += Blocked * delta;
    }

    public override void ResetStatistics()
    {
        base.ResetStatistics();
        Failures = 0;
        QueueArea = 0;
        BusyArea = 0;
        BlockedArea = 0;
    }

    private void Start(int channel, Job job)
    {
        _states[channel] = ChannelState.Busy;
        Busy++;
        _jobs[channel] = job;
        _tnext[channel] = TCurr + _delay(job);
        UpdateTNext();
    }

    private void Release(int channel)
    {
        if (_states[channel] == ChannelState.Busy)
            Busy--;
        else
            Blocked--;
        _states[channel] = ChannelState.Free;
        _jobs[channel] = null;
        _tnext[channel] = double.PositiveInfinity;
        if (_queue.Count > 0)
            Start(channel, _queue.Dequeue());
        UpdateTNext();
    }

    private void UpdateTNext()
    {
        var tnext = double.PositiveInfinity;
        foreach (var time in _tnext)
            tnext = Math.Min(tnext, time);
        TNext = tnext;
    }
}
