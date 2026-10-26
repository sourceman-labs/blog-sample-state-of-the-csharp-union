# What C# 15 changes (and what it does not)

Counterpart to `src/Domain.Before/WhyItHurts.md`. All compiler claims are checked against .NET 11 RC1.

## Files

| Concept | File |
|---|---|
| State as a union: `union OrderState(Draft, Paid, Shipped, Cancelled)`, wrapped in `record struct Order(OrderId Id, OrderState State)` | `Order.cs` |
| Core method with no guards: `Ship(Paid, TrackingNumber)` | `Order.cs` (`OrderService`) |
| Error union: `union ShipError(NotPaid, AlreadyShipped, OrderCancelled, BadTrackingNumber)` | `ShipError.cs` |
| `Result<T, E>` | `Result.cs` |
| Parse, don't validate | `TrackingNumber.cs` |
| The edge: match the state, parse the input, call `Ship` | `OrderWorkflow.Ship`, `OrderWorkflow.AsPaid` |
| Exhaustive match on the result | `OrderWorkflow.ToOutcome`, `OrderWorkflow.Describe` |
| Bind | `Composition/*.cs` |
| The same states as a `closed` hierarchy | `ClosedHierarchy/Order.cs` |
| Compile errors on demand | `Pitfalls/TypeUnionPitfall.cs` (CS0457), `Pitfalls/ShipDraftPitfall.cs` (CS1503) |

## Not new in C# 15

Some of the good parts of this sample work in C# 14 too. `Domain.Before/ImmutableRecords` has them all:

- One immutable record per state, so a `Shipped` always has a `TrackingNumber` and readers need no `!`.
- Parameter types as guards: `Ship(Paid paid, TrackingNumber trackingNumber)` cannot get a draft or a blank
  tracking number. `Ship(draft, ...)` is CS1503 (`dotnet build src/Domain.After -p:DefineConstants=DEMO_SHIP_DRAFT`).
- Parse, don't validate: `TrackingNumber` has no public constructor.
- Errors that carry data: `AlreadyShipped.TrackingNumber`, `OrderCancelled.Reason`.
- Transitions that return new values: `Ship` returns a `Shipped`, and the `Paid` stays unchanged.

## What C# 15 adds

1. **Exhaustiveness is a build error.** A switch over a `union` or a `closed` hierarchy needs no `_` arm.
   Delete an arm, or add a case, and the build fails with CS8509. Adding `OnHold` to `ShipError` fails in
   `OrderWorkflow.ToOutcome`. In C# 14, the `_` arm hides both mistakes.
2. **Nested matching is exhaustive too.** `Err<ShipError>(NotPaid)` checks Ok/Err *and* every error case.
3. **`closed` is a one-keyword fix.** `Before/ImmutableRecords/OrderService.cs` and `After/ClosedHierarchy/Order.cs`
   differ only in `abstract` → `closed` and three deleted `_` arms.
4. **A union can name types that you do not own.** `union ClientError(HttpRequestException, ProblemDetailsError,
   JsonException)` (`src/Client/ClientError.cs`) puts two BCL exception types into one closed set.
   A `closed` hierarchy cannot do this, because every case must derive from its base.
5. **One type can be a case in more than one union.** `BadTrackingNumber` is the error of `TrackingNumber.Parse`
   and a case of `ShipError`. A record can have only one base class, so a `closed` hierarchy cannot do this either.
6. **Implicit conversions from each case.** `ShipError error = new NotPaid(id);` and `return Result.Ok(paid);` need no
   wrapping call.

## The gaps

1. **Unions of types, not tagged variants.** `union Result<T, E>(T, E)` fails with CS0457 when `T == E`.
   You need wrapper types `Ok<T>` / `Err<E>`.
2. **No `?` operator.** Each fallible step needs one of these:
   - early return: `if (!x.IsOk(out var v, out var e)) return Result.Err<ShipError>(e);` (no delegates)
   - `Bind`/`Map` (nested closures, captured variables)

   Early return costs nothing extra at run time. `Bind` does (`BenchmarkDotNet.Artifacts/results/`, Composition).
3. **No `From` for error widening.** The parse step needs `MapErr(static ShipError (BadTrackingNumber e) => e)`.
   `MapErr<ShipError>(...)` does not compile: with a generic `extension<T, E>` block, you give all
   type arguments or none. A lambda with an explicit return type is the short form.
4. **A union has no shared members.** If all four cases have an `Id`, `order.Id` does not compile. Each read needs a
   switch with one arm per case. A `closed` hierarchy inherits `Id` from the base. This sample moves `Id` out of the
   union instead: `record struct Order(OrderId Id, OrderState State)`. Other options:
   - **No or-pattern with variables.** `Draft { Id: var id } or Paid { Id: var id } => id` is CS8780.
     Rust writes `Draft { id } | Paid { id, .. } => id`.
   - **A property in the union body.** `public OrderId Id => this switch { Draft d => d.Id, ... };` compiles
     and stays exhaustive: a new case is CS8509 there, not at every caller.
   - **A shared interface: match on `Value`, not `this`.** With `IHasId` on every case,
     `Id => Value is IHasId h ? h.Id : throw ...` works, but the compiler does not check that a new case has an `Id`.
   - **Trap: stack overflow, no warning.** If the union itself implements `IHasId`,
     `Id => this switch { IHasId h => h.Id }` matches the union, not the case, and `Id` calls itself.
   - **A wrapper (used here).** Rust and F# have the same gap, and the usual fix there is `Order { id, state }`.
     The cost: `Ship(Paid)` does not know its own id. The edge (`OrderWorkflow.AsPaid`) reads `order.Id` for the
     errors. A `record struct` wrapper adds no allocation.
5. **The `default` hole.** `default(TrackingNumber)`, `default(OrderState)`, and `default(Result<,>)` all compile.
   Their contents are null, and the compiler still treats `Value` as not-null.
6. **`union` declarations box value types.** `Value` is `object?`. `Ok<T>`/`Err<E>` are structs, so every `Result`
   boxes (24 B for a single reference payload). `src/UnionGen.Sample` shows the non-boxing pattern.
7. **Inside a generic union body, `this is Ok<T> ok` is CS8780.** Match on `Value` instead.
8. **No transitive conversions.** With `union ShipOrParseError(ShipError, string)`, `ShipOrParseError e = new NotPaid(id)`
   fails (CS0029). Convert to `ShipError` first.

## `union` or `closed`?

| Use `union` when | Use `closed` when |
|---|---|
| Some cases are types that you do not own (`int`, `HttpRequestException`) | You own every case |
| One type is a case in more than one set (`BadTrackingNumber`) | Each case belongs to one set |
| The cases share no data (`ShipError`) | The cases share data (`Id` on every state) |
| JSON should carry no discriminator (STJ writes the active case only) | JSON should carry `$type` (`InferClosedTypePolymorphism = true`) |
| You want a value type wrapper (a union is a struct) | The cases are classes anyway |

When you own all the cases and they share no data, both work. `OrderState` is an example: this sample shows it as a
union (`Order.cs`) and as a closed hierarchy (`ClosedHierarchy/Order.cs`).

Note: `closed` does not apply transitively. A non-closed subclass of a closed class is open again.
