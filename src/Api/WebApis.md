# C# 15 unions in a Web API

All claims are checked against .NET 11 RC1. `tests/Tests/ApiTests.cs` covers the behaviour.
Run the demo: `dotnet run --project src/Client`. It hosts the API in-process on a free loopback port.

## Files

| Concept | File |
|---|---|
| `Result<Shipped, ShipError>` → HTTP status | `Api/OrderEndpoints.cs` (`Ship`) |
| Union state → HTTP status | `Api/OrderEndpoints.cs` (`Pay`: only a `Draft` can be paid) |
| Union as a request body | `Api/Contracts/Payments.cs` (`PaymentMethod`), `OrderEndpoints.Pay` |
| Closed hierarchy on the wire | `Api/Contracts/Events.cs` (`OrderEvent`) |
| STJ source generation | `Api/Contracts/ApiJsonContext.cs` |
| Typed client + error union | `Client/OrderClient.cs`, `Client/ClientError.cs` |
| Console runner | `Client/Program.cs` |

## Result → HTTP status

```csharp
OrderWorkflow.Ship(order, request.TrackingNumber) switch
{
    Ok<Shipped>(var shipped) => TypedResults.Ok(store.Save(order.Id, shipped)),
    Err<ShipError>(BadTrackingNumber e) => Invalid("trackingNumber", $"'{e.Input}' is not a valid tracking number."),
    Err<ShipError>(NotPaid) => Conflict($"Order {id} is not paid."),
    Err<ShipError>(AlreadyShipped e) => Conflict($"Order {id} is already shipped ({e.TrackingNumber})."),
    Err<ShipError>(OrderCancelled e) => Conflict($"Order {id} is cancelled: {e.Reason}"),
};
```

- **Exhaustive mapping.** A fifth `ShipError` case fails the build (CS8509) until it gets a status code.
  With exceptions, a new failure mode is a 500 at run time.
- **Name clash: `Ok<T>`.** `Domain.After.Ok<T>` and `Microsoft.AspNetCore.Http.HttpResults.Ok<T>` together give
  CS0104. The endpoint file aliases the namespace: `using Http = Microsoft.AspNetCore.Http.HttpResults;`.
  Any `Result` library with an `Ok` type has this problem in ASP.NET Core.
- **Make STJ strict.** `RespectNullableAnnotations` + `RespectRequiredConstructorParameters` in
  `ConfigureHttpJsonOptions`: a missing or `null` `trackingNumber` is a 400.
  The request DTO uses primitives. `OrderWorkflow.Ship` parses the tracking number into a `TrackingNumber`.
- **The id lives outside the union.** A union has no shared members, so `Order` is
  `record struct Order(OrderId Id, OrderState State)`. The endpoints write `order.Id` and match on `order.State`.

## Union as a request body

```csharp
[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union PaymentMethod(Card, BankTransfer);   // {"number":"4242","expiryYear":2030} or {"iban":"SE45"}
```

- **Wire format: the active case only.** `{"iban":"SE45"}`. There is no envelope and no `$type`.
- Minimal APIs bind a union from the body with no `[FromBody]`. Query, route, header, and form binding are not
  supported (aspnetcore#66648).
- **Reading it needs a classifier.** Both cases are JSON objects. Without `[JsonUnion(TypeClassifier = ...)]`,
  STJ throws `JSON value type 'Object' is ambiguous for union type ...`.
- **How the structural classifier decides:** it picks the case whose property names match. `{"iban":"SE45"}` is
  enough to pick `BankTransfer`, and extra properties are ignored. A body that matches both cases, or neither, makes the
  classifier return null. STJ then throws, and Minimal APIs return a 400.
- **OpenAPI: `anyOf` with no discriminator.** Clients cannot tell the cases apart from the schema alone.
- **Trap: a `null` body is `default(PaymentMethod)`.** The union is a struct, so the framework cannot reject `null`
  the way it rejects `null` for a record class. `Value` is null. A switch with one arm per case compiles with no
  warning and throws `SwitchExpressionException` at run time (a 500). `Pay` checks `method.Value is null` first and
  returns a 400. The same applies to clients: `ReadFromJsonAsync<PaymentMethod>` on the JSON literal `null` returns
  `default`, so `?? throw` never fires.
- **Trap: number handling.** With `JsonSerializerDefaults.Web`, `NumberHandling` is `AllowReadingFromString`. So the
  JSON string `"x"` is ambiguous for `union Scalar(int, string, bool)`: `int` can read from a string too.
  With default options or `JsonNumberHandling.Strict`, it works.

## Union as a response body: not used here

A union response is valid, but the status code is always 200, and the client needs a classifier to read it.
"Order or not found" is a status code question, so `GET /orders/{id}` returns
`Results<Ok<OrderResponse>, NotFound<NotFoundInfo>>` instead.

## Closed hierarchy on the wire

```csharp
[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public closed record OrderEvent(int Id);
public sealed record OrderPlaced(int Id) : OrderEvent(Id);
public sealed record OrderPaid(int Id, string Method) : OrderEvent(Id);
public sealed record OrderShipped(int Id, string TrackingNumber) : OrderEvent(Id);
```

- Wire format: `{"$type":"OrderPaid","method":"card","id":1}`. There is no `[JsonDerivedType]` list
  to forget when you add a case. The base property (`id`) is written last.
- The discriminator is the type name and is case-sensitive: `"$type":"orderPaid"` throws `JsonException`.
  A payload without `$type` throws `NotSupportedException`, not `JsonException`.
- **OpenAPI: `anyOf` + `discriminator`** (with a `mapping`), not `oneOf`.
- **Union or closed?** Use `closed` for a wire contract: the tag is explicit, and OpenAPI clients can use the
  discriminator. Use a `union` when the shape of the JSON already tells the cases apart.

## STJ source generation

- RC1 supports source generation for unions (token-kind and structural classifier) and for closed hierarchies.
- **Trap:** a `JsonSerializerContext` uses PascalCase, case-sensitive names unless you say otherwise. Then the structural
  classifier finds no case for `{"iban":"..."}`, and closed-hierarchy properties silently stay at their default
  (`"id"` ≠ `Id`). Use `[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, ...)]`.
- RC1 API names: `JsonUnionAttribute.TypeClassifier`, `JsonUnionTypeStructuralClassifier`,
  `JsonTypeClassifierFactory` / `JsonTypeClassifierFactory<T>`, `JsonTypeClassifier` (a delegate:
  `Type? (ref Utf8JsonReader)`), `JsonTypeClassifierContext` (`UnionCases`, `DerivedTypes`),
  `JsonPolymorphicAttribute.InferClosedTypePolymorphism` and `.TypeClassifier`.

## Typed client: every failure is a value

```csharp
public union ClientError(HttpRequestException, ProblemDetailsError, JsonException);

public Task<Result<OrderShipped, ClientError>> ShipAsync(int id, ShipRequest request);
```

- Two of the cases are BCL exception types. The union names them directly, with no wrapper records.
- `CallAsync` catches `HttpRequestException` and `JsonException`, and no other code does. A timeout
  (`TaskCanceledException`) or a bug still throws.
- A non-success response becomes `ProblemDetailsError(Status, Title, Errors)`. `HttpValidationProblemDetails`
  reads both shapes, and `Errors` is empty for a plain ProblemDetails.
- The caller matches on the typed error. `Print` in `Program.cs` does it for the message, and this does it for the advice:

```csharp
var advice = clientError switch
{
    HttpRequestException => "retry later",
    ProblemDetailsError { Status: >= 500 } => "retry later",
    ProblemDetailsError => "fix the request",
    JsonException => "report a bug",
};
```

## Other findings

- `WebApplicationFactory<Program>` is CS0433 when the test project references two executables that use the
  ASP.NET Core framework. The `Client` console app references `Microsoft.AspNetCore.App` (to host the API), and its
  generated `Program` is public too. The tests use `WebApplicationFactory<ApiHost>` instead.
- With `JsonSerializerDefaults.Web`, OpenAPI types every number as `["number","string"]` with a regex `pattern`.
  This is `AllowReadingFromString` again.

## Verdict

Unions fit well *inside* the API. An exhaustive `Result` → HTTP mapping and a typed client whose errors are values
are the strongest points. *On the wire*, a union is untagged JSON. The server writes it with no extra work, but every
reader needs a classifier, and OpenAPI can only say `anyOf`. A `closed` hierarchy with an inferred `$type` is the better
wire contract. Check for `default(union)` at every deserialization boundary: the exhaustive switch does not protect
against it.
