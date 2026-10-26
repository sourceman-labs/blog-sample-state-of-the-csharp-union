using System.Diagnostics;

namespace Domain.Before;

public readonly record struct OrderId(int Value);
public readonly record struct PaymentId(string Value);

public enum OrderStatus { Draft, Paid, Shipped, Cancelled }

// One class for every state. Which fields are set depends on Status, and only the comments say so.
public sealed class Order(OrderId id)
{
    public OrderId Id { get; } = id;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public PaymentId? PaymentId { get; set; }           // set when Paid or Shipped
    public TrackingNumber? TrackingNumber { get; set; } // set when Shipped
    public string? CancelReason { get; set; }           // set when Cancelled
}

public static class OrderText
{
    public static string Describe(Order order) => order.Status switch
    {
        OrderStatus.Draft => "draft",
        OrderStatus.Paid => $"paid with {order.PaymentId!.Value.Value}",
        OrderStatus.Shipped => $"shipped, tracking {order.TrackingNumber!.Value.Value}",
        OrderStatus.Cancelled => $"cancelled: {order.CancelReason!}",
        _ => throw new UnreachableException(), // CS8524: an enum is open, (OrderStatus)42 is legal
    };
}

public enum ShipErrorCode { None, NotPaid, AlreadyShipped, Cancelled, BadTrackingNumber }

public static class Outcome
{
    public const string Shipped = "shipped";
    public const string NotPaid = "not-paid";
    public const string AlreadyShipped = "already-shipped";
    public const string Cancelled = "cancelled";
    public const string BadTrackingNumber = "bad-tracking-number";
}
