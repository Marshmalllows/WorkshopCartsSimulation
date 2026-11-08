using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class AuditExperiment
{
    private const int ReplicationCount = 10;

    public static void Run()
    {
        var variants = AssignmentVariants.All;
        var auditors = variants
            .SelectMany(variant => Enumerable.Range(1, ReplicationCount).Select(seed => (Variant: variant, Seed: seed)))
            .AsParallel()
            .AsOrdered()
            .Select(item =>
            {
                var parameters = item.Variant.Parameters();
                var auditor = new DispatchAuditor(parameters);
                var workshop = new Workshop(parameters, item.Seed);
                auditor.Attach(workshop);
                auditor.CheckBalance(workshop.Run(ExperimentSettings.SimulationTime));
                return (item.Variant, Auditor: auditor);
            })
            .ToList();

        Console.WriteLine($"Independent audit of dispatch decisions: {variants.Count} variants x {ReplicationCount} runs, " +
                          $"T = {ExperimentSettings.SimulationTime}");
        Console.WriteLine();

        var headers = new[] { "Check", "Checked", "Violations" };
        var rows = DispatchAuditor.Checks.Select(check => (IReadOnlyList<string>)
        [
            check,
            auditors.Sum(a => a.Auditor.Checked[check]).ToString("N0"),
            auditors.Sum(a => a.Auditor.Violations[check]).ToString("N0"),
        ]).ToList();
        TablePrinter.Print(headers, rows);

        Console.WriteLine();
        Console.WriteLine("Share of deliveries (cart A) and pickups (cart B) by machine");
        var machines = WorkshopParameters.DefaultMachineCount;
        var shareHeaders = new[] { "Variant", "Type" }
            .Concat(Enumerable.Range(0, machines).Select(machine => $"M{machine}"))
            .ToArray();
        var shareRows = new List<IReadOnlyList<string>>();
        foreach (var variant in variants)
        {
            var group = auditors.Where(a => a.Variant == variant).Select(a => a.Auditor).ToList();
            shareRows.Add(ShareRow(variant.Code, "A", group.Select(a => a.Deliveries).ToList(), machines));
            shareRows.Add(ShareRow(variant.Code, "B", group.Select(a => a.Pickups).ToList(), machines));
        }

        TablePrinter.Print(shareHeaders, shareRows);
    }

    private static IReadOnlyList<string> ShareRow(string code, string type, IReadOnlyList<long[]> counts, int machines)
    {
        var totals = Enumerable.Range(0, machines).Select(machine => counts.Sum(c => c[machine])).ToArray();
        var sum = (double)totals.Sum();
        return new[] { code, type }.Concat(totals.Select(total => (total / sum).ToString("F3"))).ToList();
    }
}
