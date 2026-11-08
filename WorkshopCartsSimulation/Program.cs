using System.Globalization;
using WorkshopCartsSimulation.Experiments;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "demo";
switch (mode)
{
    case "demo":
        DemoExperiment.Run();
        break;
    case "verify":
        VerificationExperiment.Run();
        break;
    case "warmup":
        WarmUpExperiment.Run();
        break;
    case "runs":
        ReplicationExperiment.Run();
        break;
    case "compare":
        ComparisonExperiment.Run();
        break;
    default:
        Console.WriteLine($"Unknown mode '{mode}'. Available modes: demo, verify, warmup, runs, compare.");
        return 1;
}

return 0;
