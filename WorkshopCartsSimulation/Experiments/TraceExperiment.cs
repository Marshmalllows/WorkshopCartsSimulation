using System.Text;
using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class TraceExperiment
{
    private const double Horizon = 3_000;
    private const int ConsoleLines = 60;
    private const string OutputFile = "trace.txt";

    public static void Run()
    {
        var parameters = new WorkshopParameters();
        var workshop = new Workshop(parameters, ExperimentSettings.Seed);
        var lines = new List<string>();
        var messages = new List<string>();

        workshop.Trace = (_, message) => messages.Add(message);
        workshop.EventCompleted = state =>
        {
            if (messages.Count == 0)
                return;

            var machines = string.Join(" ", state.Machines.Select(FormatState));
            var cartsA = string.Concat(state.CartsABusy.Select(busy => busy ? '1' : '0'));
            var cartsB = string.Concat(state.CartsBBusy.Select(busy => busy ? '1' : '0'));
            var prefix = $"{state.Time,9:F2}";
            foreach (var message in messages)
            {
                lines.Add($"{prefix} | {message}");
                prefix = new string(' ', prefix.Length);
            }

            lines.Add($"{new string(' ', prefix.Length)} |   queue = {state.WaitingUnits}, machines = [{machines}], " +
                      $"A = {cartsA}, B = {cartsB}");
            messages.Clear();
        };

        workshop.Run(Horizon);

        Console.WriteLine($"Event trace: seed {ExperimentSettings.Seed}, interval [0; {Horizon}], shared carts");
        Console.WriteLine("Machine states: F free, D awaiting delivery, P processing, W waiting for cart B, S cart B on the way");
        Console.WriteLine();
        foreach (var line in lines.Take(ConsoleLines))
            Console.WriteLine(line);

        File.WriteAllLines(OutputFile, lines, Encoding.UTF8);
        Console.WriteLine();
        Console.WriteLine($"{lines.Count} lines saved to {Path.GetFullPath(OutputFile)}");
    }

    private static char FormatState(MachineState state) => state switch
    {
        MachineState.Free => 'F',
        MachineState.AwaitingDelivery => 'D',
        MachineState.Processing => 'P',
        MachineState.AwaitingPickup => 'W',
        MachineState.PickupScheduled => 'S',
        _ => '?',
    };
}
