using WorkshopCartsSimulation.Core.Distributions;
using WorkshopCartsSimulation.Core.Network;
using WorkshopCartsSimulation.Core.Statistics;

namespace WorkshopCartsSimulation.Model;

public sealed class Workshop
{
    public const string UnitType = "unit";
    public const string CartAType = "cart A";
    public const string CartBType = "cart B";

    private readonly WorkshopParameters _parameters;
    private readonly Core.Network.Model _model = new();
    private readonly Create _arrivals;
    private readonly Process _dispatcher;
    private readonly Process _poolA;
    private readonly Process _poolB;
    private readonly Join[] _joinA;
    private readonly Process[] _delivery;
    private readonly Split[] _splitA;
    private readonly Process[] _machines;
    private readonly Process[] _returnA;
    private readonly Process[] _approachB;
    private readonly Join[] _joinB;
    private readonly Process[] _storing;
    private readonly Split[] _splitB;
    private readonly Dispose _storage;
    private readonly Dictionary<Element, int> _machineByTarget = [];
    private readonly TimeWeightedStatistic _queue = new();
    private readonly TallyStatistic _waitingTime = new();

    public Workshop(WorkshopParameters parameters, int seed)
    {
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        if (parameters.CartsA.MachineCount != parameters.MachineCount ||
            parameters.CartsB.MachineCount != parameters.MachineCount)
            throw new ArgumentException("Cart assignments must match the machine count.", nameof(parameters));

        var seeds = new Random(seed);
        var interarrival = new ExponentialVariate(parameters.MeanInterarrivalTime, new Random(seeds.Next()));
        var processing = new NormalVariate(parameters.ProcessingTimeMean, parameters.ProcessingTimeStdDev,
            new Random(seeds.Next()));
        var machines = parameters.MachineCount;
        var cartsA = parameters.CartsA.CartCount;
        var cartsB = parameters.CartsB.CartCount;

        _arrivals = _model.Add(new Create("Create", interarrival,
            (id, time) => new Job(id, UnitType, time) { ServiceTime = processing.Next() }));
        _dispatcher = _model.Add(new Process("D0", 1, _ => 0));
        _poolA = _model.Add(new Process("PA", cartsA, _ => 0));
        _poolB = _model.Add(new Process("PB", cartsB, _ => 0));
        _joinA = new Join[machines];
        _delivery = new Process[machines];
        _splitA = new Split[machines];
        _machines = new Process[machines];
        _returnA = new Process[machines];
        _approachB = new Process[machines];
        _joinB = new Join[machines];
        _storing = new Process[machines];
        _splitB = new Split[machines];
        for (var i = 0; i < machines; i++)
        {
            var loaded = parameters.LoadedTravelTime(i);
            var empty = parameters.EmptyTravelTime(i);
            _joinA[i] = _model.Add(new Join($"JA{i}", 2));
            _delivery[i] = _model.Add(new Process($"TA{i}", 1, _ => loaded));
            _splitA[i] = _model.Add(new Split($"SA{i}"));
            _machines[i] = _model.Add(new Process($"M{i}", 1, job => job.ServiceTime, maxQueue: 0));
            _returnA[i] = _model.Add(new Process($"RA{i}", cartsA, _ => empty));
            _approachB[i] = _model.Add(new Process($"CB{i}", cartsB, _ => empty));
            _joinB[i] = _model.Add(new Join($"JB{i}", 2));
            _storing[i] = _model.Add(new Process($"DB{i}", 1, _ => loaded));
            _splitB[i] = _model.Add(new Split($"SB{i}"));
        }

        _storage = _model.Add(new Dispose("Dispose"));
        Connect(machines);

        for (var cart = 0; cart < cartsA; cart++)
            _poolA.InAct(new Job(cart, CartAType, 0));
        for (var cart = 0; cart < cartsB; cart++)
            _poolB.InAct(new Job(cart, CartBType, 0));

        _model.RouteChosen = OnRouteChosen;
        _model.InstantProcessed = OnInstantProcessed;
        _model.StatisticsReset = OnStatisticsReset;
    }

    public double Time => _model.TCurr;

    public Action<CartDispatch>? CartDispatched { get; set; }

    public Action<WorkshopSnapshot>? EventCompleted { get; set; }

    public Action<double, string>? Trace
    {
        get => _model.Trace;
        set => _model.Trace = value;
    }

    public WorkshopResult Run(double endTime, double warmUpTime = 0)
    {
        _model.Simulate(endTime, warmUpTime);
        return CollectResult();
    }

    public IReadOnlyList<WorkshopSample> RunWithSampling(double endTime, double sampleInterval)
    {
        if (sampleInterval <= 0)
            throw new ArgumentOutOfRangeException(nameof(sampleInterval), "Sample interval must be positive.");

        var samples = new List<WorkshopSample>();
        var previous = (Time: 0.0, Queue: 0.0, Processed: 0L, TimeInSystem: 0.0);
        var cartCount = _parameters.CartsA.CartCount + _parameters.CartsB.CartCount;

        void TakeSample(double time)
        {
            var current = (Time: time, Queue: _queue.Integral(time), Processed: _storage.Quantity,
                TimeInSystem: _storage.TimeInSystem.Sum);
            var duration = current.Time - previous.Time;
            var processed = current.Processed - previous.Processed;
            var revenue = processed * _parameters.ProfitPerUnit;
            var cartCost = cartCount * _parameters.CartCostPerTimeUnit * duration;

            samples.Add(new WorkshopSample(
                time,
                (current.Queue - previous.Queue) / duration,
                processed,
                processed > 0 ? (current.TimeInSystem - previous.TimeInSystem) / processed : double.NaN,
                (revenue - cartCost) / duration));
            previous = current;
        }

        _model.Simulate(endTime, 0, sampleInterval, TakeSample);
        return samples;
    }

    public WorkshopSnapshot Snapshot()
    {
        var machines = Enumerable.Range(0, _parameters.MachineCount).Select(StateOf).ToArray();
        return new WorkshopSnapshot(
            _model.TCurr,
            WaitingUnits,
            machines,
            BusyCarts(_poolA, _parameters.CartsA.CartCount),
            BusyCarts(_poolB, _parameters.CartsB.CartCount),
            PickupCandidates().ToArray());
    }

    private int WaitingUnits => _dispatcher.QueueLength + _dispatcher.Occupied;

    private void Connect(int machines)
    {
        _arrivals.AddRoute(_dispatcher);
        for (var i = 0; i < machines; i++)
        {
            var machine = i;
            var priority = machines - machine;
            _dispatcher.AddRoute(_joinA[i], priority, _ => _joinA[machine].Count == 0);
            _poolA.AddRoute(_joinA[i], priority, cart => NextDelivery() != (machine, (int)cart.Id));
            _joinA[i].AddRoute(_delivery[i]);
            _delivery[i].AddRoute(_splitA[i]);
            _splitA[i].AddRoute(_machines[i], jobType: UnitType);
            _splitA[i].AddRoute(_returnA[i], jobType: CartAType);
            _returnA[i].AddRoute(_poolA);
            _machines[i].AddRoute(_joinB[i], block: _ => _joinB[machine].Count == 0);
            _poolB.AddRoute(_approachB[i], block: cart => NextPickup() != (machine, (int)cart.Id));
            _approachB[i].AddRoute(_joinB[i]);
            _joinB[i].AddRoute(_storing[i]);
            _storing[i].AddRoute(_splitB[i]);
            _splitB[i].AddRoute(_storage, jobType: UnitType);
            _splitB[i].AddRoute(_poolB, jobType: CartBType);
            _machineByTarget[_joinA[i]] = i;
            _machineByTarget[_approachB[i]] = i;
        }
    }

    private (int Machine, int Cart) NextDelivery()
    {
        if (_dispatcher.Blocked == 0)
            return (-1, -1);
        foreach (var join in _joinA)
            if (join.Count > 0)
                return (-1, -1);

        for (var machine = 0; machine < _parameters.MachineCount; machine++)
        {
            if (!IsMachineFree(machine))
                continue;
            var cart = FirstFreeCart(_poolA, _parameters.CartsA, machine);
            if (cart >= 0)
                return (machine, cart);
        }

        return (-1, -1);
    }

    private (int Machine, int Cart) NextPickup()
    {
        var best = (Machine: -1, Cart: -1);
        for (var machine = 0; machine < _parameters.MachineCount; machine++)
        {
            if (!IsPickupCandidate(machine) || (best.Machine >= 0 &&
                    _machines[machine].BlockedSince(0) >= _machines[best.Machine].BlockedSince(0)))
                continue;
            var cart = FirstFreeCart(_poolB, _parameters.CartsB, machine);
            if (cart >= 0)
                best = (machine, cart);
        }

        return best;
    }

    private IEnumerable<int> PickupCandidates() =>
        Enumerable.Range(0, _parameters.MachineCount)
            .Where(IsPickupCandidate)
            .OrderBy(machine => _machines[machine].BlockedSince(0));

    private bool IsPickupCandidate(int machine) =>
        _machines[machine].Blocked > 0 && _approachB[machine].Occupied == 0 && _joinB[machine].Count == 0;

    private bool IsMachineFree(int machine) =>
        _joinA[machine].Count == 0 && _delivery[machine].Occupied == 0 && _machines[machine].Occupied == 0;

    private static int FirstFreeCart(Process pool, CartAssignment assignment, int machine)
    {
        var best = -1;
        for (var channel = 0; channel < pool.Channels; channel++)
        {
            var cart = pool.JobAt(channel);
            if (cart is null)
                continue;
            var number = (int)cart.Id;
            if (assignment.CanServe(number, machine) && (best < 0 || number < best))
                best = number;
        }

        return best;
    }

    private static bool[] BusyCarts(Process pool, int count)
    {
        var busy = Enumerable.Repeat(true, count).ToArray();
        foreach (var cart in pool.Jobs)
            busy[cart.Id] = false;
        return busy;
    }

    private MachineState StateOf(int machine)
    {
        var device = _machines[machine];
        if (device.Busy > 0)
            return MachineState.Processing;
        if (device.Blocked > 0)
            return _approachB[machine].Occupied > 0 || _joinB[machine].Count > 0
                ? MachineState.PickupScheduled
                : MachineState.AwaitingPickup;
        return _joinA[machine].Count > 0 || _delivery[machine].Occupied > 0
            ? MachineState.AwaitingDelivery
            : MachineState.Free;
    }

    private void OnRouteChosen(Element from, Job job, Route route)
    {
        if (from == _dispatcher)
            _waitingTime.Add(_model.TCurr - job.CreatedTime);

        if (CartDispatched is null || (from != _poolA && from != _poolB))
            return;

        var type = from == _poolA ? CartType.A : CartType.B;
        CartDispatched(new CartDispatch(type, (int)job.Id, _machineByTarget[route.Target], Snapshot()));
    }

    private void OnInstantProcessed(double time)
    {
        _queue.Update(time, WaitingUnits);
        EventCompleted?.Invoke(Snapshot());
    }

    private void OnStatisticsReset(double time)
    {
        _queue.Reset(time);
        _waitingTime.Reset();
    }

    private WorkshopResult CollectResult()
    {
        var time = _model.TCurr;
        var duration = _model.ObservationTime;
        var cartCount = _parameters.CartsA.CartCount + _parameters.CartsB.CartCount;
        var revenue = _storage.Quantity * _parameters.ProfitPerUnit;
        var cartCost = cartCount * _parameters.CartCostPerTimeUnit * duration;

        return new WorkshopResult
        {
            ObservationTime = duration,
            ArrivedUnits = _arrivals.Quantity,
            ProcessedUnits = _storage.Quantity,
            UnitsInSystemAtEnd = _arrivals.Created - _storage.Total,
            MeanQueueLength = _queue.Mean(time),
            MaxQueueLength = _queue.Max,
            MeanWaitingTime = _waitingTime.Mean,
            MeanTimeInSystem = _storage.TimeInSystem.Mean,
            MachineOccupancy = Enumerable.Range(0, _parameters.MachineCount)
                .Select(i => (_delivery[i].BusyArea + _machines[i].BusyArea + _machines[i].BlockedArea) / duration)
                .ToArray(),
            MachineProcessing = _machines.Select(m => m.MeanBusy).ToArray(),
            MachineBlocked = _machines.Select(m => m.MeanBlocked).ToArray(),
            CartAUtilization = 1 - (_poolA.BusyArea + _poolA.BlockedArea) / duration / _parameters.CartsA.CartCount,
            CartBUtilization = 1 - (_poolB.BusyArea + _poolB.BlockedArea) / duration / _parameters.CartsB.CartCount,
            Revenue = revenue,
            CartCost = cartCost,
        };
    }
}
