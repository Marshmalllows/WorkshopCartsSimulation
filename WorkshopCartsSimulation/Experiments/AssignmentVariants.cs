using WorkshopCartsSimulation.Model;

namespace WorkshopCartsSimulation.Experiments;

internal static class AssignmentVariants
{
    public static IReadOnlyList<AssignmentVariant> All { get; } = Create();

    private static AssignmentVariant[] Create()
    {
        var machines = WorkshopParameters.DefaultMachineCount;
        var shared = CartAssignment.Shared(3, machines);
        var pairs = CartAssignment.Dedicated(machines, [0, 1], [2, 3], [4]);
        var nearSingle = CartAssignment.Dedicated(machines, [0], [1, 2], [3, 4]);
        var overlapping = CartAssignment.Dedicated(machines, [0, 1, 2], [1, 2, 3], [2, 3, 4]);

        return
        [
            new("V1", "all shared", shared, shared),
            new("V2", "dedicated {0,1}{2,3}{4}", pairs, pairs),
            new("V3", "A shared, B dedicated", shared, pairs),
            new("V4", "A dedicated, B shared", pairs, shared),
            new("V5", "dedicated {0}{1,2}{3,4}", nearSingle, nearSingle),
            new("V6", "overlapping {0-2}{1-3}{2-4}", overlapping, overlapping),
        ];
    }
}
