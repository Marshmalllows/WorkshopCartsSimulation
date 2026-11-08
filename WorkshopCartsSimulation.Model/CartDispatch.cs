namespace WorkshopCartsSimulation.Model;

public sealed record CartDispatch(CartType Type, int Cart, int Machine, WorkshopSnapshot Before);
