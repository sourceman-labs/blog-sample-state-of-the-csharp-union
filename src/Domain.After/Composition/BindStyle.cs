namespace Domain.After.Composition;

public static class BindStyle
{
    public static Result<Shipped, ShipError> Ship(Order order, string rawTrackingNumber) =>
        OrderWorkflow.AsPaid(order).Bind(paid =>
            TrackingNumber.Parse(rawTrackingNumber)
                .MapErr(static ShipError (BadTrackingNumber e) => e)
                .Map(trackingNumber => OrderService.Ship(paid, trackingNumber)));

    public static string Handle(Order order, string rawTrackingNumber) =>
        OrderWorkflow.ToOutcome(Ship(order, rawTrackingNumber));
}
