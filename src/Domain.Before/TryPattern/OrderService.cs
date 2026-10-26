using System.Diagnostics;

namespace Domain.Before.TryPattern;

public static class OrderService
{
    public static bool TryShip(Order order, string trackingNumber, out ShipErrorCode error)
    {
        if (order.Status == OrderStatus.Draft) { error = ShipErrorCode.NotPaid; return false; }
        if (order.Status == OrderStatus.Shipped) { error = ShipErrorCode.AlreadyShipped; return false; }
        if (order.Status == OrderStatus.Cancelled) { error = ShipErrorCode.Cancelled; return false; }
        if (!TrackingNumber.TryParse(trackingNumber, out var parsed)) { error = ShipErrorCode.BadTrackingNumber; return false; }

        order.TrackingNumber = parsed;
        order.Status = OrderStatus.Shipped;
        error = ShipErrorCode.None;
        return true;
    }

    public static string Handle(Order order, string trackingNumber)
    {
        if (TryShip(order, trackingNumber, out var error)) return Outcome.Shipped;

        return error switch
        {
            ShipErrorCode.NotPaid => Outcome.NotPaid,
            ShipErrorCode.AlreadyShipped => Outcome.AlreadyShipped,
            ShipErrorCode.Cancelled => Outcome.Cancelled,
            ShipErrorCode.BadTrackingNumber => Outcome.BadTrackingNumber,
            _ => throw new UnreachableException($"Unexpected error code {error}."),
        };
    }
}
