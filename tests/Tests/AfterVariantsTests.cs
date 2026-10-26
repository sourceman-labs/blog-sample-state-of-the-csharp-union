using Domain.After;
using Domain.After.Composition;
using Closed = Domain.After.ClosedHierarchy;
using Status = Domain.Before.OrderStatus;

namespace Tests;

public class AfterVariantsTests
{
    public static TheoryData<Status, string, string> Cases => BeforeVariantsTests.Cases;

    private static readonly OrderId Id = new(1);
    private static readonly PaymentId Payment = new("PAY-1");

    private static readonly TrackingNumber Track0 = Track("TRACK-0");

    private static Order Load(Status status) => status switch
    {
        Status.Draft => new(Id, new Draft()),
        Status.Paid => new(Id, new Paid(Payment)),
        Status.Shipped => new(Id, new Shipped(Payment, Track0)),
        Status.Cancelled => new(Id, new Cancelled("changed mind")),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    private static Closed.Order LoadClosed(Status status) => status switch
    {
        Status.Draft => new Closed.Draft(Id),
        Status.Paid => new Closed.Paid(Id, Payment),
        Status.Shipped => new Closed.Shipped(Id, Payment, Track0),
        Status.Cancelled => new Closed.Cancelled(Id, "changed mind"),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void All_variants_agree_with_before(Status status, string trackingNumber, string expected)
    {
        Assert.Equal(expected, OrderWorkflow.Handle(Load(status), trackingNumber));
        Assert.Equal(expected, BindStyle.Handle(Load(status), trackingNumber));
        Assert.Equal(expected, Closed.OrderService.Handle(LoadClosed(status), trackingNumber));
    }

    [Fact]
    public void A_paid_order_ships_with_its_payment_and_the_tracking_number()
    {
        var paid = OrderService.Pay(new Draft(), Payment);

        Assert.True(OrderWorkflow.Ship(new Order(Id, paid), "TRACK-1").IsOk(out var shipped, out _));
        Assert.Equal(new Shipped(Payment, Track("TRACK-1")), shipped);
    }

    [Fact]
    public void Errors_carry_their_data()
    {
        Assert.Equal(new AlreadyShipped(Id, Track0), Error(OrderWorkflow.Ship(Load(Status.Shipped), "TRACK-2")).Value);
        Assert.Equal(new OrderCancelled(Id, "changed mind"), Error(OrderWorkflow.Ship(Load(Status.Cancelled), "TRACK-1")).Value);
        Assert.Equal(new BadTrackingNumber(" "), Error(OrderWorkflow.Ship(Load(Status.Paid), " ")).Value);
    }

    [Fact]
    public void Describe_uses_the_data_of_each_case()
    {
        Assert.Equal("shipped, tracking TRACK-0", OrderWorkflow.Describe(Load(Status.Shipped)));
        Assert.Equal("cancelled: changed mind", OrderWorkflow.Describe(Load(Status.Cancelled)));
        Assert.Equal("shipped, tracking TRACK-0", Closed.OrderService.Describe(LoadClosed(Status.Shipped)));
    }

    [Fact]
    public void Default_result_holds_no_value()
    {
        var result = default(Result<int, string>);

        Assert.Null(result.Value);
        Assert.Throws<InvalidOperationException>(() => result.IsOk(out _, out _));
    }

    [Fact]
    public void Default_tracking_number_and_default_order_are_representable()
    {
        Assert.Null(default(TrackingNumber).Value);
        Assert.Null(default(Order).State.Value);
    }

    private static TrackingNumber Track(string value) =>
        TrackingNumber.Parse(value).IsOk(out var t, out _) ? t : throw new InvalidOperationException("Valid input.");

    private static ShipError Error(Result<Shipped, ShipError> result) =>
        result.IsOk(out _, out var error) ? throw new InvalidOperationException("Expected an error.") : error;
}
