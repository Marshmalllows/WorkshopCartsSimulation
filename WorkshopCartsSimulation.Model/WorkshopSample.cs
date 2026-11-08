namespace WorkshopCartsSimulation.Model;

public sealed record WorkshopSample(
    double Time,
    double MeanQueueLength,
    long ProcessedUnits,
    double MeanTimeInSystem,
    double ProfitPerTimeUnit);
