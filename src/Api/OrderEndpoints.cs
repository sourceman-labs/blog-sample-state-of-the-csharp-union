using Api.Contracts;
using Domain.After;
using Http = Microsoft.AspNetCore.Http.HttpResults;

namespace Api;

internal static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/orders");
        orders.MapPost("/", Place);
        orders.MapGet("/{id:int}", Lookup);
        orders.MapGet("/{id:int}/events", Events);
        orders.MapPost("/{id:int}/payments", Pay);
        orders.MapPost("/{id:int}/shipments", Ship);
        return app;
    }

    private static Http.Created<OrderResponse> Place(OrderStore store)
    {
        var order = store.Place();
        return TypedResults.Created($"/orders/{order.Id}", Response(order));
    }

    private static Http.Results<Http.Ok<OrderResponse>, Http.NotFound<NotFoundInfo>> Lookup(int id, OrderStore store) =>
        store.TryGet(new OrderId(id), out var order) ? TypedResults.Ok(Response(order)) : NotFound(id);

    private static OrderEvent[] Events(int id, OrderStore store) => store.Events(id);

    private static Http.Results<Http.Ok<OrderPaid>, Http.NotFound<NotFoundInfo>, Http.ValidationProblem, Http.ProblemHttpResult> Pay(
        int id, PaymentMethod method, OrderStore store)
    {
        // A JSON null body binds to default(PaymentMethod). The switch below compiles without a null arm, then throws.
        if (method.Value is null) return Invalid("$", "A payment method is required.");
        if (!store.TryGet(new OrderId(id), out var order)) return NotFound(id);

        var name = method switch
        {
            Card => "card",
            BankTransfer => "bank-transfer",
        };

        return order.State switch
        {
            Draft draft => TypedResults.Ok(store.Save(order.Id, OrderService.Pay(draft, new PaymentId($"{name}-{id}")), name)),
            Paid or Shipped or Cancelled => Conflict($"Order {id} is {State(order)}, not a draft."),
        };
    }

    private static Http.Results<Http.Ok<OrderShipped>, Http.NotFound<NotFoundInfo>, Http.ValidationProblem, Http.ProblemHttpResult> Ship(
        int id, ShipRequest request, OrderStore store)
    {
        if (!store.TryGet(new OrderId(id), out var order)) return NotFound(id);

        return OrderWorkflow.Ship(order, request.TrackingNumber) switch
        {
            Ok<Shipped>(var shipped) => TypedResults.Ok(store.Save(order.Id, shipped)),
            Err<ShipError>(BadTrackingNumber e) => Invalid("trackingNumber", $"'{e.Input}' is not a valid tracking number."),
            Err<ShipError>(NotPaid) => Conflict($"Order {id} is not paid."),
            Err<ShipError>(AlreadyShipped e) => Conflict($"Order {id} is already shipped ({e.TrackingNumber})."),
            Err<ShipError>(OrderCancelled e) => Conflict($"Order {id} is cancelled: {e.Reason}"),
        };
    }

    private static OrderResponse Response(Order order) => new(order.Id.Value, State(order));

    private static string State(Order order) => order.State switch
    {
        Draft => "draft",
        Paid => "paid",
        Shipped => "shipped",
        Cancelled => "cancelled",
    };

    private static Http.NotFound<NotFoundInfo> NotFound(int id) => TypedResults.NotFound(new NotFoundInfo(id, $"No order {id}."));

    private static Http.ProblemHttpResult Conflict(string title) =>
        TypedResults.Problem(title: title, statusCode: StatusCodes.Status409Conflict);

    private static Http.ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
