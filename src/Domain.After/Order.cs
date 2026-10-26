namespace Domain.After;

public sealed record Draft;
public sealed record Paid(PaymentId PaymentId);
public sealed record Shipped(PaymentId PaymentId, TrackingNumber TrackingNumber);
public sealed record Cancelled(string Reason);

// Each state carries exactly its own data. A Shipped order without a tracking number cannot exist.
public union OrderState(Draft, Paid, Shipped, Cancelled);

// A union has no shared members, so the data that every state has lives outside the union.
// A struct, like the union: wrapping the state adds no allocation.
public readonly record struct Order(OrderId Id, OrderState State);

public static class OrderService
{
    // The Draft parameter is the guard: only a draft can be paid.
    public static Paid Pay(Draft draft, PaymentId paymentId) => new(paymentId);

    // No guard clauses: the parameter types are the guards.
    public static Shipped Ship(Paid paid, TrackingNumber trackingNumber) => new(paid.PaymentId, trackingNumber);
}
