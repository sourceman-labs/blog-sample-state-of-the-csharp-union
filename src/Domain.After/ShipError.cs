namespace Domain.After;

public sealed record NotPaid(OrderId Id);
public sealed record AlreadyShipped(OrderId Id, TrackingNumber TrackingNumber);
public sealed record OrderCancelled(OrderId Id, string Reason);
public sealed record BadTrackingNumber(string Input);

public union ShipError(NotPaid, AlreadyShipped, OrderCancelled, BadTrackingNumber);
