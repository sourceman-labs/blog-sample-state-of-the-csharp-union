using Domain.After;
using Status = Domain.Before.OrderStatus;
using After = Domain.After;
using Before = Domain.Before;
using Closed = Domain.After.ClosedHierarchy;
using Immutable = Domain.Before.ImmutableRecords;

namespace Benchmarks;

internal readonly record struct OrderInput(Status Status, string TrackingNumber);

// Each call loads the order first, the way a handler loads it from storage. The Before variants change the order
// they ship, so they cannot share one instance across calls. Every variant loads, so the comparison stays fair.
internal static class OrderInputs
{
    public const int Count = 1000;

    private static readonly OrderInput Valid = new(Status.Paid, "TRACK-1");

    private static readonly OrderInput[] Failures =
    [
        new(Status.Draft, "TRACK-1"),
        new(Status.Shipped, "TRACK-1"),
        new(Status.Cancelled, "TRACK-1"),
        new(Status.Paid, " "),
    ];

    private static readonly Before.OrderId BeforeId = new(1);
    private static readonly Before.PaymentId BeforePayment = new("PAY-1");
    private static readonly Before.TrackingNumber BeforeShipped =
        Before.TrackingNumber.TryParse("TRACK-0", out var bt) ? bt : throw new InvalidOperationException();

    private static readonly OrderId Id = new(1);
    private static readonly PaymentId Payment = new("PAY-1");

    private static readonly TrackingNumber Shipped =
        TrackingNumber.TryParse("TRACK-0", out var t) ? t : throw new InvalidOperationException();

    public static OrderInput[] Create(int failurePercent)
    {
        var inputs = new OrderInput[Count];
        int failures = Count * failurePercent / 100;
        for (int i = 0; i < Count; i++) inputs[i] = i < failures ? Failures[i % Failures.Length] : Valid;
        new Random(42).Shuffle(inputs);
        return inputs;
    }

    public static Before.Order LoadBefore(Status s) => s switch
    {
        Status.Draft => new(BeforeId),
        Status.Paid => new(BeforeId) { Status = Status.Paid, PaymentId = BeforePayment },
        Status.Shipped => new(BeforeId) { Status = Status.Shipped, PaymentId = BeforePayment, TrackingNumber = BeforeShipped },
        Status.Cancelled => new(BeforeId) { Status = Status.Cancelled, CancelReason = "changed mind" },
        _ => throw new ArgumentOutOfRangeException(nameof(s)),
    };

    public static Immutable.Order LoadImmutable(Status s) => s switch
    {
        Status.Draft => new Immutable.Draft(BeforeId),
        Status.Paid => new Immutable.Paid(BeforeId, BeforePayment),
        Status.Shipped => new Immutable.Shipped(BeforeId, BeforePayment, BeforeShipped),
        Status.Cancelled => new Immutable.Cancelled(BeforeId, "changed mind"),
        _ => throw new ArgumentOutOfRangeException(nameof(s)),
    };

    public static After.Order LoadAfter(Status s) => s switch
    {
        Status.Draft => new(Id, new After.Draft()),
        Status.Paid => new(Id, new After.Paid(Payment)),
        Status.Shipped => new(Id, new After.Shipped(Payment, Shipped)),
        Status.Cancelled => new(Id, new After.Cancelled("changed mind")),
        _ => throw new ArgumentOutOfRangeException(nameof(s)),
    };

    public static Closed.Order LoadClosed(Status s) => s switch
    {
        Status.Draft => new Closed.Draft(Id),
        Status.Paid => new Closed.Paid(Id, Payment),
        Status.Shipped => new Closed.Shipped(Id, Payment, Shipped),
        Status.Cancelled => new Closed.Cancelled(Id, "changed mind"),
        _ => throw new ArgumentOutOfRangeException(nameof(s)),
    };
}
