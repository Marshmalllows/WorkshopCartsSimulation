namespace WorkshopCartsSimulation.Core.Network;

public sealed class Split : Element
{
    public Split(string name) : base(name)
    {
    }

    public override void InAct(Job job)
    {
        if (job.Members.Count == 0)
            throw new InvalidOperationException($"{Name} received {job}, which is not a group.");

        Quantity++;
        foreach (var member in job.Members)
        {
            var route = ChooseRoute(member)
                        ?? throw new InvalidOperationException($"{Name} has no open route for {member}.");
            Send(member, route);
        }
    }
}
