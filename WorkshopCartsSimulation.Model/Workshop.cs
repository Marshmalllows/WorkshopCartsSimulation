using WorkshopCartsSimulation.Core.Distributions;
using WorkshopCartsSimulation.Core.Simulation;

namespace WorkshopCartsSimulation.Model;

public sealed class Workshop
{
    private readonly WorkshopParameters _parameters;
    private readonly Simulator _simulator = new();
    private readonly IRandomVariate _interarrivalTime;
    private readonly IRandomVariate _processingTime;
    private readonly Queue<Unit> _waitingUnits = new();
    private readonly MachineState[] _machineStates;
    private readonly Unit?[] _unitOnMachine;
    private readonly List<int> _machinesAwaitingPickup = [];
    private readonly bool[] _cartABusy;
    private readonly bool[] _cartBBusy;
    private readonly WorkshopStatistics _statistics;
    private long _unitsInSystem;

    public Workshop(WorkshopParameters parameters, int seed)
    {
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        if (parameters.CartsA.MachineCount != parameters.MachineCount ||
            parameters.CartsB.MachineCount != parameters.MachineCount)
            throw new ArgumentException("Cart assignments must match the machine count.", nameof(parameters));

        var seeds = new Random(seed);
        _interarrivalTime = new ExponentialVariate(parameters.MeanInterarrivalTime, new Random(seeds.Next()));
        _processingTime = new NormalVariate(parameters.ProcessingTimeMean, parameters.ProcessingTimeStdDev,
            new Random(seeds.Next()));
        _machineStates = new MachineState[parameters.MachineCount];
        _unitOnMachine = new Unit?[parameters.MachineCount];
        _cartABusy = new bool[parameters.CartsA.CartCount];
        _cartBBusy = new bool[parameters.CartsB.CartCount];
        _statistics = new WorkshopStatistics(parameters.MachineCount);
    }

    public double Time => _simulator.Time;

    public WorkshopResult Run(double endTime, double warmUpTime = 0)
    {
        if (warmUpTime < 0 || warmUpTime >= endTime)
            throw new ArgumentOutOfRangeException(nameof(warmUpTime), "Warm-up time must be in [0, endTime).");

        _simulator.Schedule(_interarrivalTime.Next(), Arrive);
        if (warmUpTime > 0)
            _simulator.Schedule(warmUpTime, () => _statistics.Reset(_simulator.Time));
        _simulator.Run(endTime, AfterEvent);
        return CollectResult();
    }

    public IReadOnlyList<WorkshopSample> RunWithSampling(double endTime, double sampleInterval)
    {
        if (sampleInterval <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleInterval), "Sample interval must be positive.");

        var samples = new List<WorkshopSample>();
        var previous = new SamplePoint(0, 0, 0, 0);
        var cartCount = _parameters.CartsA.CartCount + _parameters.CartsB.CartCount;

        void TakeSample()
        {
            var time = _simulator.Time;
            var current = new SamplePoint(
                time,
                _statistics.QueueLength.Integral(time),
                _statistics.ProcessedUnits,
                _statistics.TimeInSystem.Sum);
            var duration = current.Time - previous.Time;
            var processed = current.ProcessedUnits - previous.ProcessedUnits;
            var revenue = processed * _parameters.ProfitPerUnit;
            var cartCost = cartCount * _parameters.CartCostPerTimeUnit * duration;

            samples.Add(new WorkshopSample(
                time,
                (current.QueueIntegral - previous.QueueIntegral) / duration,
                processed,
                processed > 0 ? (current.TimeInSystemSum - previous.TimeInSystemSum) / processed : double.NaN,
                (revenue - cartCost) / duration));

            previous = current;
            if (time + sampleInterval <= endTime)
                _simulator.Schedule(sampleInterval, TakeSample);
        }

        _simulator.Schedule(_interarrivalTime.Next(), Arrive);
        _simulator.Schedule(sampleInterval, TakeSample);
        _simulator.Run(endTime, AfterEvent);
        return samples;
    }

    private void AfterEvent()
    {
        DispatchCartsA();
        DispatchCartsB();
        _statistics.Observe(
            _simulator.Time,
            _waitingUnits.Count,
            _machineStates,
            _cartABusy.Count(busy => busy),
            _cartBBusy.Count(busy => busy));
    }

    private void Arrive()
    {
        _statistics.ArrivedUnits++;
        _unitsInSystem++;
        _waitingUnits.Enqueue(new Unit(_simulator.Time, _processingTime.Next()));
        _simulator.Schedule(_interarrivalTime.Next(), Arrive);
    }

    private void DispatchCartsA()
    {
        while (_waitingUnits.Count > 0 && TryFindDelivery(out var machine, out var cart))
        {
            var unit = _waitingUnits.Dequeue();
            _statistics.WaitingTime.Add(_simulator.Time - unit.ArrivalTime);
            _machineStates[machine] = MachineState.AwaitingDelivery;
            _unitOnMachine[machine] = unit;
            _cartABusy[cart] = true;

            var loaded = _parameters.LoadedTravelTime(machine);
            var empty = _parameters.EmptyTravelTime(machine);
            _simulator.Schedule(loaded, () => StartProcessing(machine, unit.ProcessingTime));
            _simulator.Schedule(loaded + empty, () => _cartABusy[cart] = false);
        }
    }

    private bool TryFindDelivery(out int machine, out int cart)
    {
        for (machine = 0; machine < _parameters.MachineCount; machine++)
        {
            if (_machineStates[machine] != MachineState.Free)
                continue;
            cart = FindFreeCart(_cartABusy, _parameters.CartsA, machine);
            if (cart >= 0)
                return true;
        }

        machine = -1;
        cart = -1;
        return false;
    }

    private void StartProcessing(int machine, double processingTime)
    {
        _machineStates[machine] = MachineState.Processing;
        _simulator.Schedule(processingTime, () => FinishProcessing(machine));
    }

    private void FinishProcessing(int machine)
    {
        _machineStates[machine] = MachineState.AwaitingPickup;
        _machinesAwaitingPickup.Add(machine);
    }

    private void DispatchCartsB()
    {
        for (var i = 0; i < _machinesAwaitingPickup.Count;)
        {
            var machine = _machinesAwaitingPickup[i];
            var cart = FindFreeCart(_cartBBusy, _parameters.CartsB, machine);
            if (cart < 0)
            {
                i++;
                continue;
            }

            _machinesAwaitingPickup.RemoveAt(i);
            _machineStates[machine] = MachineState.PickupScheduled;
            _cartBBusy[cart] = true;

            var empty = _parameters.EmptyTravelTime(machine);
            var loaded = _parameters.LoadedTravelTime(machine);
            _simulator.Schedule(empty, () => PickUp(machine, cart, loaded));
        }
    }

    private void PickUp(int machine, int cart, double travelToStorage)
    {
        var unit = _unitOnMachine[machine]!;
        _unitOnMachine[machine] = null;
        _machineStates[machine] = MachineState.Free;
        _simulator.Schedule(travelToStorage, () => Store(unit, cart));
    }

    private void Store(Unit unit, int cart)
    {
        _statistics.ProcessedUnits++;
        _statistics.TimeInSystem.Add(_simulator.Time - unit.ArrivalTime);
        _unitsInSystem--;
        _cartBBusy[cart] = false;
    }

    private static int FindFreeCart(bool[] busy, CartAssignment assignment, int machine)
    {
        for (var cart = 0; cart < busy.Length; cart++)
        {
            if (!busy[cart] && assignment.CanServe(cart, machine))
                return cart;
        }

        return -1;
    }

    private WorkshopResult CollectResult()
    {
        var time = _simulator.Time;
        var duration = time - _statistics.StartTime;
        var cartCount = _parameters.CartsA.CartCount + _parameters.CartsB.CartCount;
        var revenue = _statistics.ProcessedUnits * _parameters.ProfitPerUnit;
        var cartCost = cartCount * _parameters.CartCostPerTimeUnit * duration;

        return new WorkshopResult
        {
            ObservationTime = duration,
            ArrivedUnits = _statistics.ArrivedUnits,
            ProcessedUnits = _statistics.ProcessedUnits,
            UnitsInSystemAtEnd = _unitsInSystem,
            MeanQueueLength = _statistics.QueueLength.Mean(time),
            MaxQueueLength = _statistics.QueueLength.Max,
            MeanWaitingTime = _statistics.WaitingTime.Mean,
            MeanTimeInSystem = _statistics.TimeInSystem.Mean,
            MachineOccupancy = _statistics.MachineOccupied.Select(s => s.Mean(time)).ToArray(),
            MachineProcessing = _statistics.MachineProcessing.Select(s => s.Mean(time)).ToArray(),
            CartAUtilization = _statistics.BusyCartsA.Mean(time) / _parameters.CartsA.CartCount,
            CartBUtilization = _statistics.BusyCartsB.Mean(time) / _parameters.CartsB.CartCount,
            Revenue = revenue,
            CartCost = cartCost,
        };
    }

    private sealed record Unit(double ArrivalTime, double ProcessingTime);

    private sealed record SamplePoint(double Time, double QueueIntegral, long ProcessedUnits, double TimeInSystemSum);
}
