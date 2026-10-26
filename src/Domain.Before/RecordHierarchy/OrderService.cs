using System.Diagnostics;

namespace Domain.Before.RecordHierarchy;

public abstract record ShipResult;
public sealed record OrderShipped(Order Order) : ShipResult;
public sealed record NotPaid(OrderId Id) : ShipResult;
public sealed record AlreadyShipped(OrderId Id) : ShipResult;
public sealed record OrderCancelled(OrderId Id) : ShipResult;
public sealed record BadTrackingNumber(string Input) : ShipResult;

public static class OrderService
{
    public static ShipResult Ship(Order order, string trackingNumber)
    {
        if (order.Status == OrderStatus.Draft) return new NotPaid(order.Id);
        if (order.Status == OrderStatus.Shipped) return new AlreadyShipped(order.Id);
        if (order.Status == OrderStatus.Cancelled) return new OrderCancelled(order.Id);
        if (!TrackingNumber.TryParse(trackingNumber, out var parsed)) return new BadTrackingNumber(trackingNumber);

        order.TrackingNumber = parsed;
        order.Status = OrderStatus.Shipped;
        return new OrderShipped(order);
    }

    public static string Handle(Order order, string trackingNumber) =>
        Ship(order, trackingNumber) switch
        {
            OrderShipped => Outcome.Shipped,
            NotPaid => Outcome.NotPaid,
            AlreadyShipped => Outcome.AlreadyShipped,
            OrderCancelled => Outcome.Cancelled,
            BadTrackingNumber => Outcome.BadTrackingNumber,
            // Required by CS8509. Delete the BadTrackingNumber arm above and it still compiles.
            _ => throw new UnreachableException(),
        };
}
