using Domain.Before;
using Immutable = Domain.Before.ImmutableRecords;

namespace Tests;

public class BeforeVariantsTests
{
    public static TheoryData<OrderStatus, string, string> Cases => new()
    {
        { OrderStatus.Paid, "TRACK-1", Outcome.Shipped },
        { OrderStatus.Draft, "TRACK-1", Outcome.NotPaid },
        { OrderStatus.Shipped, "TRACK-1", Outcome.AlreadyShipped },
        { OrderStatus.Cancelled, "TRACK-1", Outcome.Cancelled },
        { OrderStatus.Paid, "", Outcome.BadTrackingNumber },
        { OrderStatus.Paid, "   ", Outcome.BadTrackingNumber },
        { OrderStatus.Draft, "", Outcome.NotPaid },
        { OrderStatus.Cancelled, " ", Outcome.Cancelled },
    };

    private static readonly OrderId Id = new(1);
    private static readonly PaymentId Payment = new("PAY-1");
    private static readonly TrackingNumber Track0 = TrackingNumber.TryParse("TRACK-0", out var t) ? t : throw new InvalidOperationException();

    public static Order Load(OrderStatus status) => status switch
    {
        OrderStatus.Draft => new(Id),
        OrderStatus.Paid => new(Id) { Status = OrderStatus.Paid, PaymentId = Payment },
        OrderStatus.Shipped => new(Id) { Status = OrderStatus.Shipped, PaymentId = Payment, TrackingNumber = Track0 },
        OrderStatus.Cancelled => new(Id) { Status = OrderStatus.Cancelled, CancelReason = "changed mind" },
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static Immutable.Order LoadImmutable(OrderStatus status) => status switch
    {
        OrderStatus.Draft => new Immutable.Draft(Id),
        OrderStatus.Paid => new Immutable.Paid(Id, Payment),
        OrderStatus.Shipped => new Immutable.Shipped(Id, Payment, Track0),
        OrderStatus.Cancelled => new Immutable.Cancelled(Id, "changed mind"),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void All_variants_agree(OrderStatus status, string trackingNumber, string expected)
    {
        Assert.Equal(expected, Domain.Before.Exceptions.OrderService.Handle(Load(status), trackingNumber));
        Assert.Equal(expected, Domain.Before.TryPattern.OrderService.Handle(Load(status), trackingNumber));
        Assert.Equal(expected, Domain.Before.RecordHierarchy.OrderService.Handle(Load(status), trackingNumber));
        Assert.Equal(expected, Domain.Before.ResultStruct.OrderService.Handle(Load(status), trackingNumber));
        Assert.Equal(expected, Immutable.OrderService.Handle(LoadImmutable(status), trackingNumber));
    }

    [Fact]
    public void Immutable_records_need_no_bang_in_C_sharp_14()
    {
        Assert.Equal("shipped, tracking TRACK-0", Immutable.OrderService.Describe(LoadImmutable(OrderStatus.Shipped)));
        Assert.Equal("cancelled: changed mind", Immutable.OrderService.Describe(LoadImmutable(OrderStatus.Cancelled)));
    }

    [Fact]
    public void Ship_changes_the_order_in_place()
    {
        var order = Load(OrderStatus.Paid);

        Domain.Before.Exceptions.OrderService.Ship(order, "TRACK-1");

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.Equal("shipped, tracking TRACK-1", OrderText.Describe(order));
    }

    [Fact]
    public void An_invalid_state_compiles_and_fails_at_run_time()
    {
        var order = new Order(Id) { Status = OrderStatus.Shipped }; // no tracking number

        Assert.Throws<InvalidOperationException>(() => OrderText.Describe(order));
    }

    [Fact]
    public void Default_result_is_an_err_with_a_null_error()
    {
        var result = default(Domain.Before.ResultStruct.Result<Order, string>);

        Assert.False(result.IsOk);
        Assert.Null(result.Error);
    }
}
