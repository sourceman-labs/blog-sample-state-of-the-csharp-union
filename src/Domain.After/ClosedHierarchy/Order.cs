namespace Domain.After.ClosedHierarchy;

// Domain.Before.ImmutableRecords with `closed` instead of `abstract`: the `_` arms are gone, and a missing arm is CS8509.
// The cases share Id through the base record, so there is no wrapper.
public closed record Order(OrderId Id);
public sealed record Draft(OrderId Id) : Order(Id);
public sealed record Paid(OrderId Id, PaymentId PaymentId) : Order(Id);
public sealed record Shipped(OrderId Id, PaymentId PaymentId, TrackingNumber TrackingNumber) : Order(Id);
public sealed record Cancelled(OrderId Id, string Reason) : Order(Id);

public closed record ShipResult;
public sealed record OrderShipped(Shipped Order) : ShipResult;
public sealed record NotPaid(OrderId Id) : ShipResult;
public sealed record AlreadyShipped(OrderId Id, TrackingNumber TrackingNumber) : ShipResult;
public sealed record OrderCancelled(OrderId Id, string Reason) : ShipResult;
public sealed record BadTrackingNumber(string Input) : ShipResult;

public static class OrderService
{
    public static ShipResult Ship(Order order, string trackingNumber) => order switch
    {
        Draft d => new NotPaid(d.Id),
        Shipped s => new AlreadyShipped(s.Id, s.TrackingNumber),
        Cancelled c => new OrderCancelled(c.Id, c.Reason),
        Paid p when TrackingNumber.TryParse(trackingNumber, out var t) => new OrderShipped(new Shipped(p.Id, p.PaymentId, t)),
        Paid => new BadTrackingNumber(trackingNumber),
    };

    public static string Handle(Order order, string trackingNumber) =>
        Ship(order, trackingNumber) switch
        {
            OrderShipped => Outcome.Shipped,
            NotPaid => Outcome.NotPaid,
            AlreadyShipped => Outcome.AlreadyShipped,
            OrderCancelled => Outcome.Cancelled,
            BadTrackingNumber => Outcome.BadTrackingNumber,
        };

    public static string Describe(Order order) => order switch
    {
        Draft => "draft",
        Paid p => $"paid with {p.PaymentId.Value}",
        Shipped s => $"shipped, tracking {s.TrackingNumber.Value}",
        Cancelled c => $"cancelled: {c.Reason}",
    };
}
