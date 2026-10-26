using System.Runtime.CompilerServices;
using Domain.After;
using UnionGen;

namespace Benchmarks.Pipeline;

// The three union rows share one body, so only the return type differs. They use TrackingNumber.TryParse, not Parse,
// so the only Result is the one that each row returns.

internal static class DeclaredUnion
{
    public static Result<Shipped, ShipError> Ship(Order order, string trackingNumber) => order.State switch
    {
        Draft => Result.Err<ShipError>(new NotPaid(order.Id)),
        Shipped s => Result.Err<ShipError>(new AlreadyShipped(order.Id, s.TrackingNumber)),
        Cancelled c => Result.Err<ShipError>(new OrderCancelled(order.Id, c.Reason)),
        Paid p when TrackingNumber.TryParse(trackingNumber, out var t) => Result.Ok(new Shipped(p.PaymentId, t)),
        Paid => Result.Err<ShipError>(new BadTrackingNumber(trackingNumber)),
    };

    public static string Handle(Order order, string trackingNumber) => Ship(order, trackingNumber) switch
    {
        Ok<Shipped> => Outcome.Shipped,
        Err<ShipError>(NotPaid) => Outcome.NotPaid,
        Err<ShipError>(AlreadyShipped) => Outcome.AlreadyShipped,
        Err<ShipError>(OrderCancelled) => Outcome.Cancelled,
        Err<ShipError>(BadTrackingNumber) => Outcome.BadTrackingNumber,
    };
}

[Union]
internal readonly struct HandWrittenShipResult : IUnion
{
    private readonly Ok<Shipped> _ok;
    private readonly Err<ShipError> _err;
    private readonly byte _tag;

    public HandWrittenShipResult(Ok<Shipped> value) { _tag = 1; _ok = value; }
    public HandWrittenShipResult(Err<ShipError> value) { _tag = 2; _err = value; }

    public object? Value => _tag switch { 1 => _ok, 2 => _err, _ => null };
    public bool HasValue => _tag != 0;
    public bool TryGetValue(out Ok<Shipped> value) { value = _ok; return _tag == 1; }
    public bool TryGetValue(out Err<ShipError> value) { value = _err; return _tag == 2; }
}

internal static class HandWrittenUnion
{
    public static HandWrittenShipResult Ship(Order order, string trackingNumber) => order.State switch
    {
        Draft => Result.Err<ShipError>(new NotPaid(order.Id)),
        Shipped s => Result.Err<ShipError>(new AlreadyShipped(order.Id, s.TrackingNumber)),
        Cancelled c => Result.Err<ShipError>(new OrderCancelled(order.Id, c.Reason)),
        Paid p when TrackingNumber.TryParse(trackingNumber, out var t) => Result.Ok(new Shipped(p.PaymentId, t)),
        Paid => Result.Err<ShipError>(new BadTrackingNumber(trackingNumber)),
    };

    public static string Handle(Order order, string trackingNumber) => Ship(order, trackingNumber) switch
    {
        Ok<Shipped> => Outcome.Shipped,
        Err<ShipError>(NotPaid) => Outcome.NotPaid,
        Err<ShipError>(AlreadyShipped) => Outcome.AlreadyShipped,
        Err<ShipError>(OrderCancelled) => Outcome.Cancelled,
        Err<ShipError>(BadTrackingNumber) => Outcome.BadTrackingNumber,
    };
}

[ValueUnion, UnionCase<Ok<Shipped>>, UnionCase<Err<ShipError>>]
internal readonly partial struct GeneratedShipResult;

internal static class GeneratedUnion
{
    public static GeneratedShipResult Ship(Order order, string trackingNumber) => order.State switch
    {
        Draft => Result.Err<ShipError>(new NotPaid(order.Id)),
        Shipped s => Result.Err<ShipError>(new AlreadyShipped(order.Id, s.TrackingNumber)),
        Cancelled c => Result.Err<ShipError>(new OrderCancelled(order.Id, c.Reason)),
        Paid p when TrackingNumber.TryParse(trackingNumber, out var t) => Result.Ok(new Shipped(p.PaymentId, t)),
        Paid => Result.Err<ShipError>(new BadTrackingNumber(trackingNumber)),
    };

    public static string Handle(Order order, string trackingNumber) => Ship(order, trackingNumber) switch
    {
        Ok<Shipped> => Outcome.Shipped,
        Err<ShipError>(NotPaid) => Outcome.NotPaid,
        Err<ShipError>(AlreadyShipped) => Outcome.AlreadyShipped,
        Err<ShipError>(OrderCancelled) => Outcome.Cancelled,
        Err<ShipError>(BadTrackingNumber) => Outcome.BadTrackingNumber,
    };
}
