using System.Text.Json;
using Api;
using Api.Contracts;
using Client;
using Domain.After;

await using var api = ApiHost.Build(["--urls=http://127.0.0.1:0", "--Logging:LogLevel:Default=None"]);
await api.StartAsync();

using var http = new HttpClient { BaseAddress = new Uri(api.Urls.First()) };
var client = new OrderClient(http);

Section("POST /orders, /payments, /shipments: Result<Shipped, ShipError> → 200 / 400 / 409");
Print("place", await client.PlaceAsync(), o => $"order {o.Id} is {o.State}");
Print("ship a draft", await client.ShipAsync(1, new("TRACK-1")), s => $"shipped with {s.TrackingNumber}");
Print("pay by card", await client.PayAsync(1, new Card("4242424242424242", 2030)), p => $"paid by {p.Method}");
Print("ship, blank tracking", await client.ShipAsync(1, new(" ")), s => $"shipped with {s.TrackingNumber}");
Print("ship", await client.ShipAsync(1, new("TRACK-1")), s => $"shipped with {s.TrackingNumber}");
Print("ship again", await client.ShipAsync(1, new("TRACK-2")), s => $"shipped with {s.TrackingNumber}");

Section("GET /orders/{id}: 200 or 404");
Print("lookup 1", await client.LookupAsync(1), o => $"order {o.Id} is {o.State}");
Print("lookup 99", await client.LookupAsync(99), o => $"order {o.Id} is {o.State}");

Section("POST /orders/{id}/payments: a union as the request body (structural classifier)");
Print("place", await client.PlaceAsync(), o => $"order {o.Id} is {o.State}");
Print("pay by bank", await client.PayAsync(2, new BankTransfer("SE4550000000058398257466")), p => $"paid by {p.Method}");

Section("GET /orders/1/events: a closed hierarchy with an inferred $type");
Print("events", await client.EventsAsync(1), events => string.Join(" | ", events.Select(e => e switch
{
    OrderPlaced => "placed",
    OrderPaid p => $"paid by {p.Method}",
    OrderShipped s => $"shipped with {s.TrackingNumber}",
})));

Section("Nothing listens on the port: the HttpRequestException is a ClientError case");
using var nowhere = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") };
var failed = await new OrderClient(nowhere).PlaceAsync();
Print("place", failed, o => $"order {o.Id}");
if (!failed.IsOk(out _, out var clientError))
{
    var advice = clientError switch
    {
        HttpRequestException => "retry later",
        ProblemDetailsError { Status: >= 500 } => "retry later",
        ProblemDetailsError => "fix the request",
        JsonException => "report a bug",
    };
    Console.WriteLine($"  advice: {advice}");
}

Section("OpenAPI: the union is anyOf, the closed hierarchy is anyOf + discriminator");
using var openApi = JsonDocument.Parse(await http.GetStringAsync("/openapi/v1.json"));
var schemas = openApi.RootElement.GetProperty("components").GetProperty("schemas");
foreach (var name in new[] { "PaymentMethod", "OrderEvent" })
    Console.WriteLine($"  {name}: {JsonSerializer.Serialize(schemas.GetProperty(name))}");

await api.StopAsync();

static void Section(string title) => Console.WriteLine($"\n== {title}");

static void Print<T>(string label, Result<T, ClientError> result, Func<T, string> ok)
{
    var text = result.IsOk(out var value, out var error) ? ok(value) : error switch
    {
        HttpRequestException e => $"HTTP request failed: {e.Message}",
        ProblemDetailsError { Errors.Count: 0 } e => $"{e.Status} {e.Title}",
        ProblemDetailsError e => $"{e.Status} {e.Title} [{string.Join("; ", e.Errors.Select(x => $"{x.Key}: {string.Join(", ", x.Value)}"))}]",
        JsonException e => $"response body is not valid: {e.Message}",
    };
    Console.WriteLine($"  {label}: {text}");
}
