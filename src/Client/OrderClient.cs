using System.Net.Http.Json;
using System.Text.Json;
using Api.Contracts;
using Domain.After;
using Microsoft.AspNetCore.Http;

namespace Client;

public sealed class OrderClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public Task<Result<OrderResponse, ClientError>> PlaceAsync() =>
        CallAsync<OrderResponse>(() => http.PostAsync("/orders", null));

    public Task<Result<OrderResponse, ClientError>> LookupAsync(int id) =>
        CallAsync<OrderResponse>(() => http.GetAsync($"/orders/{id}"));

    public Task<Result<OrderEvent[], ClientError>> EventsAsync(int id) =>
        CallAsync<OrderEvent[]>(() => http.GetAsync($"/orders/{id}/events"));

    public Task<Result<OrderPaid, ClientError>> PayAsync(int id, PaymentMethod method) =>
        CallAsync<OrderPaid>(() => http.PostAsJsonAsync($"/orders/{id}/payments", method, Options));

    public Task<Result<OrderShipped, ClientError>> ShipAsync(int id, ShipRequest request) =>
        CallAsync<OrderShipped>(() => http.PostAsJsonAsync($"/orders/{id}/shipments", request, Options));

    // The I/O boundary: exceptions from HttpClient and STJ become ClientError cases here, and only here.
    private static async Task<Result<T, ClientError>> CallAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        HttpResponseMessage response;
        try { response = await send(); }
        catch (HttpRequestException e) { return Result.Err<ClientError>(e); }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return Result.Err(await ProblemAsync(response));

            T? body;
            try { body = await response.Content.ReadFromJsonAsync<T>(Options); }
            catch (JsonException e) { return Result.Err<ClientError>(e); }

            if (body is null)
                return Result.Err<ClientError>(new JsonException($"The response body is null, not a {typeof(T).Name}."));

            return Result.Ok(body);
        }
    }

    private static async Task<ClientError> ProblemAsync(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;
        if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
            return new ProblemDetailsError(status, response.ReasonPhrase ?? "", new Dictionary<string, string[]>());

        HttpValidationProblemDetails? problem;
        try { problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(Options); }
        catch (JsonException e) { return e; }

        var title = problem?.Title ?? response.ReasonPhrase ?? "";
        var errors = problem?.Errors.ToDictionary(e => e.Key, e => e.Value ?? []) ?? [];
        return new ProblemDetailsError(status, title, errors);
    }
}
