namespace Domain.Before.Exceptions;

public sealed class OrderNotPaidException(OrderId id) : InvalidOperationException($"Order {id.Value} is not paid.");
public sealed class OrderAlreadyShippedException(OrderId id) : InvalidOperationException($"Order {id.Value} is already shipped.");
public sealed class OrderCancelledException(OrderId id) : InvalidOperationException($"Order {id.Value} is cancelled.");

public static class OrderService
{
    /// <exception cref="OrderNotPaidException">The order is a draft.</exception>
    /// <exception cref="OrderAlreadyShippedException">The order is shipped.</exception>
    /// <exception cref="OrderCancelledException">The order is cancelled.</exception>
    /// <exception cref="ArgumentException">The tracking number is blank.</exception>
    public static void Ship(Order order, string trackingNumber)
    {
        if (order.Status == OrderStatus.Draft) throw new OrderNotPaidException(order.Id);
        if (order.Status == OrderStatus.Shipped) throw new OrderAlreadyShippedException(order.Id);
        if (order.Status == OrderStatus.Cancelled) throw new OrderCancelledException(order.Id);
        if (!TrackingNumber.TryParse(trackingNumber, out var parsed))
            throw new ArgumentException("The tracking number is blank.", nameof(trackingNumber));

        order.TrackingNumber = parsed;
        order.Status = OrderStatus.Shipped;
    }

    public static string Handle(Order order, string trackingNumber)
    {
        try
        {
            Ship(order, trackingNumber);
            return Outcome.Shipped;
        }
        catch (OrderNotPaidException) { return Outcome.NotPaid; }
        catch (OrderAlreadyShippedException) { return Outcome.AlreadyShipped; }
        catch (OrderCancelledException) { return Outcome.Cancelled; }
        catch (ArgumentException) { return Outcome.BadTrackingNumber; }
    }
}
