namespace WorkshopCartsSimulation.Core.Simulation;

public sealed class Simulator
{
    private readonly PriorityQueue<Action, (double Time, long Sequence)> _calendar = new();
    private long _sequence;

    public double Time { get; private set; }

    public long ProcessedEvents { get; private set; }

    public void Schedule(double delay, Action action)
    {
        if (delay < 0)
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative.");
        ArgumentNullException.ThrowIfNull(action);
        _calendar.Enqueue(action, (Time + delay, _sequence++));
    }

    public void Run(double endTime, Action? afterEvent = null)
    {
        if (endTime < Time)
            throw new ArgumentOutOfRangeException(nameof(endTime), "End time cannot be earlier than current time.");

        while (_calendar.TryPeek(out _, out var key) && key.Time <= endTime)
        {
            var action = _calendar.Dequeue();
            Time = key.Time;
            action();
            ProcessedEvents++;
            afterEvent?.Invoke();
        }

        Time = endTime;
    }
}
