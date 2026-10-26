#if DEMO_SHIP_DRAFT
namespace Domain.After.Pitfalls;

public static class ShipDraftPitfall
{
    // CS1503: a Draft is not a Paid. Pay first.
    public static Shipped Ship(Draft order, TrackingNumber trackingNumber) => OrderService.Ship(order, trackingNumber);
}
#endif
