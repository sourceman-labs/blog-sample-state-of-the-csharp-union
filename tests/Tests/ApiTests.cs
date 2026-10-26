using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api;
using Api.Contracts;
using Client;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests;

public sealed class ApiTests : IDisposable
{
    private readonly WebApplicationFactory<ApiHost> _factory = new();
    private readonly HttpClient _http;
    private readonly OrderClient _client;

    public ApiTests()
    {
        _http = _factory.CreateClient();
        _client = new OrderClient(_http);
    }

    public void Dispose()
    {
        _http.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Place_pay_ship_walks_the_lifecycle()
    {
        Assert.True((await _client.PlaceAsync()).IsOk(out var placed, out _));
        Assert.True((await _client.PayAsync(placed.Id, new Card("4242", 2030))).IsOk(out var paid, out _));
        Assert.True((await _client.ShipAsync(placed.Id, new("TRACK-1"))).IsOk(out var shipped, out _));

        Assert.Equal(new OrderResponse(1, "draft"), placed);
        Assert.Equal(new OrderPaid(1, "card"), paid);
        Assert.Equal(new OrderShipped(1, "TRACK-1"), shipped);
    }

    [Fact]
    public async Task Bad_tracking_number_is_a_validation_problem()
    {
        await _client.PlaceAsync();
        await _client.PayAsync(1, new Card("4242", 2030));

        var result = await _client.ShipAsync(1, new(" "));

        Assert.False(result.IsOk(out _, out var error));
        var problem = Assert.IsType<ProblemDetailsError>(error.Value);
        Assert.Equal(400, problem.Status);
        Assert.Equal(["trackingNumber"], problem.Errors.Keys);
    }

    [Fact]
    public async Task Wrong_state_is_409()
    {
        await _client.PlaceAsync();

        var shipDraft = await _client.ShipAsync(1, new("TRACK-1"));
        await _client.PayAsync(1, new Card("4242", 2030));
        var payTwice = await _client.PayAsync(1, new Card("4242", 2030));
        await _client.ShipAsync(1, new("TRACK-1"));
        var shipTwice = await _client.ShipAsync(1, new("TRACK-2"));

        Assert.Equal(409, Status(shipDraft));
        Assert.Equal(409, Status(payTwice));
        Assert.Equal(409, Status(shipTwice));
    }

    [Fact]
    public async Task Unknown_order_is_404()
    {
        Assert.Equal(404, Status(await _client.LookupAsync(99)));
        Assert.Equal(404, Status(await _client.ShipAsync(99, new("TRACK-1"))));
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"trackingNumber":null}""")]
    [InlineData("null")]
    public async Task Malformed_request_is_400(string json)
    {
        await _client.PlaceAsync();

        using var response = await _http.PostAsync("/orders/1/shipments", new StringContent(json, null, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Union_is_written_as_the_active_case_only()
    {
        var json = JsonSerializer.Serialize(new PaymentMethod(new BankTransfer("SE45")), ApiJsonContext.Default.PaymentMethod);

        Assert.Equal("""{"iban":"SE45"}""", json);
    }

    [Fact]
    public async Task Union_request_body_picks_the_case_from_the_properties()
    {
        await _client.PlaceAsync();
        await _client.PlaceAsync();

        Assert.True((await _client.PayAsync(1, new Card("4242", 2030))).IsOk(out var byCard, out _));
        Assert.True((await _client.PayAsync(2, new BankTransfer("SE45"))).IsOk(out var byBank, out _));

        Assert.Equal("card", byCard.Method);
        Assert.Equal("bank-transfer", byBank.Method);
    }

    [Theory]
    [InlineData("""{"iban":"SE45","number":"1","expiryYear":1}""")]
    [InlineData("42")]
    [InlineData("null")]
    public async Task Ambiguous_or_null_union_body_is_400(string json)
    {
        await _client.PlaceAsync();

        using var response = await _http.PostAsync("/orders/1/payments", new StringContent(json, null, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Closed_hierarchy_round_trips_with_an_inferred_type_discriminator()
    {
        await _client.PlaceAsync();
        await _client.PayAsync(1, new Card("4242", 2030));
        await _client.ShipAsync(1, new("TRACK-1"));

        var json = await _http.GetStringAsync("/orders/1/events");
        Assert.True((await _client.EventsAsync(1)).IsOk(out var events, out _));

        Assert.Contains("\"$type\":\"OrderPlaced\"", json);
        Assert.Contains("\"$type\":\"OrderPaid\"", json);
        Assert.Contains("\"$type\":\"OrderShipped\"", json);
        Assert.Equal([typeof(OrderPlaced), typeof(OrderPaid), typeof(OrderShipped)], events.Select(e => e.GetType()));
    }

    [Fact]
    public async Task OpenApi_describes_the_union_as_anyOf()
    {
        using var doc = JsonDocument.Parse(await _http.GetStringAsync("/openapi/v1.json"));
        var payment = doc.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("PaymentMethod");

        var refs = payment.GetProperty("anyOf").EnumerateArray().Select(e => e.GetProperty("$ref").GetString());
        Assert.Equal(["#/components/schemas/Card", "#/components/schemas/BankTransfer"], refs);
        Assert.False(payment.TryGetProperty("discriminator", out _));
    }

    [Fact]
    public async Task Connection_failure_is_an_HttpRequestException_case()
    {
        using var http = new HttpClient(new Stub(_ => throw new HttpRequestException("refused"))) { BaseAddress = new("http://x") };

        var result = await new OrderClient(http).PlaceAsync();

        Assert.False(result.IsOk(out _, out var error));
        Assert.Equal("refused", Assert.IsType<HttpRequestException>(error.Value).Message);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("""{"unknown":1}""")]
    public async Task Bad_response_body_is_a_JsonException_case(string json)
    {
        using var http = new HttpClient(new Stub(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, null, "application/json") }))
        {
            BaseAddress = new("http://x"),
        };

        var result = await new OrderClient(http).LookupAsync(1);

        Assert.False(result.IsOk(out _, out var error));
        Assert.IsType<JsonException>(error.Value);
    }

    [Fact]
    public async Task Null_field_errors_in_a_problem_become_empty_lists()
    {
        const string json = """{"title":"Bad","status":400,"errors":{"trackingNumber":null}}""";
        using var http = new HttpClient(new Stub(_ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent(json, null, "Application/Problem+JSON") }))
        {
            BaseAddress = new("http://x"),
        };

        var result = await new OrderClient(http).LookupAsync(1);

        Assert.False(result.IsOk(out _, out var error));
        var problem = Assert.IsType<ProblemDetailsError>(error.Value);
        Assert.Equal("Bad", problem.Title);
        Assert.Empty(problem.Errors["trackingNumber"]);
    }

    [Fact]
    public void Source_generated_context_handles_unions_and_closed_hierarchies()
    {
        var method = JsonSerializer.Deserialize("""{"iban":"SE45"}""", ApiJsonContext.Default.PaymentMethod);
        var events = JsonSerializer.Deserialize("""[{"$type":"OrderPaid","id":1,"method":"card"}]""", ApiJsonContext.Default.OrderEventArray);

        Assert.Equal(new BankTransfer("SE45"), method.Value);
        Assert.Equal([new OrderPaid(1, "card")], events!);
    }

    private static int Status<T>(Domain.After.Result<T, ClientError> result) =>
        result.IsOk(out _, out var error) ? 200 : Assert.IsType<ProblemDetailsError>(error.Value).Status;

    private sealed class Stub(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
