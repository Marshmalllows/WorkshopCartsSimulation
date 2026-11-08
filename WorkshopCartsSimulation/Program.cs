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
    case "runlength":
        RunLengthExperiment.Run();
        break;
    case "audit":
        AuditExperiment.Run();
        break;
    case "trace":
        TraceExperiment.Run();
        break;
    case "load":
        LoadExperiment.Run();
        break;
    case "carts":
        CartCountExperiment.Run();
        break;
    default:
        Console.WriteLine($"Unknown mode '{mode}'. Available modes: demo, verify, warmup, runlength, runs, compare, audit, trace, load, carts.");
        return 1;
}

return 0;
