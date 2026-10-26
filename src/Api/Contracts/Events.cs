namespace Api.Contracts;

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record OrderEvent(int Id);

public sealed record OrderPlaced(int Id) : OrderEvent(Id);
public sealed record OrderPaid(int Id, string Method) : OrderEvent(Id);
public sealed record OrderShipped(int Id, string TrackingNumber) : OrderEvent(Id);
