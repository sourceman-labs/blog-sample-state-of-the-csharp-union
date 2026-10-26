using System.Diagnostics;

namespace Domain.Before.ResultStruct;

public static class OrderService
{
    public static Result<Order, ShipErrorCode> Ship(Order order, string trackingNumber)
    {
        if (order.Status == OrderStatus.Draft) return ShipErrorCode.NotPaid;
        if (order.Status == OrderStatus.Shipped) return ShipErrorCode.AlreadyShipped;
        if (order.Status == OrderStatus.Cancelled) return ShipErrorCode.Cancelled;
        if (!TrackingNumber.TryParse(trackingNumber, out var parsed)) return ShipErrorCode.BadTrackingNumber;

        order.TrackingNumber = parsed;
        order.Status = OrderStatus.Shipped;
        return order;
    }

    public static string Handle(Order order, string trackingNumber) =>
        Ship(order, trackingNumber).Match(
            static _ => Outcome.Shipped,
            static error => error switch
            {
                ShipErrorCode.NotPaid => Outcome.NotPaid,
                ShipErrorCode.AlreadyShipped => Outcome.AlreadyShipped,
                ShipErrorCode.Cancelled => Outcome.Cancelled,
                ShipErrorCode.BadTrackingNumber => Outcome.BadTrackingNumber,
                _ => throw new UnreachableException($"Unexpected error code {error}."),
            });
}
