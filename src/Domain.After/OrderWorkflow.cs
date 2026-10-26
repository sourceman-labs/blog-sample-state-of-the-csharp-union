namespace Domain.After;

public static class OrderWorkflow
{
    // The edge: an order from storage can be in any state, and the tracking number is raw input.
    // Each step can fail with one ShipError case.
    public static Result<Shipped, ShipError> Ship(Order order, string rawTrackingNumber)
    {
        if (!AsPaid(order).IsOk(out var paid, out var wrongState))
            return Result.Err(wrongState);

        // The type argument widens BadTrackingNumber to ShipError. Without it, Err<BadTrackingNumber> does not convert.
        if (!TrackingNumber.Parse(rawTrackingNumber).IsOk(out var trackingNumber, out var badInput))
            return Result.Err<ShipError>(badInput);

        return Result.Ok(OrderService.Ship(paid, trackingNumber));
    }

    public static Result<Paid, ShipError> AsPaid(Order order) => order.State switch
    {
        Paid paid => Result.Ok(paid),
        Draft => Result.Err<ShipError>(new NotPaid(order.Id)),
        Shipped s => Result.Err<ShipError>(new AlreadyShipped(order.Id, s.TrackingNumber)),
        Cancelled c => Result.Err<ShipError>(new OrderCancelled(order.Id, c.Reason)),
    };

    public static string Handle(Order order, string rawTrackingNumber) =>
        ToOutcome(Ship(order, rawTrackingNumber));

    // Exhaustive with no `_` arm: a new ShipError case fails the build here.
    internal static string ToOutcome(Result<Shipped, ShipError> result) => result switch
    {
        Ok<Shipped> => Outcome.Shipped,
        Err<ShipError>(NotPaid) => Outcome.NotPaid,
        Err<ShipError>(AlreadyShipped) => Outcome.AlreadyShipped,
        Err<ShipError>(OrderCancelled) => Outcome.Cancelled,
        Err<ShipError>(BadTrackingNumber) => Outcome.BadTrackingNumber,
    };

    // Exhaustive, and no `!`: each case has its own data.
    public static string Describe(Order order) => order.State switch
    {
        Draft => "draft",
        Paid p => $"paid with {p.PaymentId}",
        Shipped s => $"shipped, tracking {s.TrackingNumber}",
        Cancelled c => $"cancelled: {c.Reason}",
    };
}
