# Why the "before" variants hurt

The project compiles as C# 14 (`<LangVersion>14</LangVersion>`), with CS8509/CS8524 as errors.
Every variant implements `Ship(order, trackingNumber)` with the same four failures: NotPaid, AlreadyShipped,
Cancelled, BadTrackingNumber. The ids are `readonly record struct`s, and `TrackingNumber` has no public constructor
(`TryParse` only), the same as in `Domain.After`.

The first four variants are the common styles: one mutable `Order` class (`Shared.cs`) plus a way to report failure.
The fifth, `ImmutableRecords`, is the best model that C# 14 has. It is the fair baseline for C# 15.

| | Exceptions | Try-pattern | Record result | Hand-rolled `Result<T,E>` | Immutable records |
|---|---|---|---|---|---|
| File | `Exceptions/` | `TryPattern/` | `RecordHierarchy/` | `ResultStruct/` | `ImmutableRecords/` |
| Failure visible in signature | No (`void`) | Partly (`bool` + enum) | Yes | Yes | Yes |
| Error carries data | Message string | No | Yes | Only if `E` does | Yes |
| Invalid state possible | Yes | Yes | Yes | Yes | No |
| Readers need `!` | Yes | Yes | Yes | Yes | No |
| Compiler finds a missing case | No | No | No | No | **No** |
| Catch-all arm needed | n/a | Yes (CS8524) | Yes (CS8509) | Yes (via `E`) | **Yes (CS8509)** |

## The problems of the common styles

1. **One class for every state.** `Order` has `PaymentId`, `TrackingNumber`, and `CancelReason`, all nullable.
   Which ones are set depends on `Status`, and only comments say so. `new Order(id) { Status = Shipped }` compiles.
2. **Readers need `!`.** `OrderText.Describe` writes `order.TrackingNumber!.Value.Value`, because the type cannot say
   that a shipped order has one. With the invalid state from point 1, it throws `InvalidOperationException`.
3. **Exceptions hide the contract.** `Ship` returns `void`. The four failures live in XML docs and in three custom
   exception types. Delete a `catch` and it still compiles.
4. **Exceptions are slow on the failure path.** Each throw allocates and captures a stack trace (see `BenchmarkDotNet.Artifacts/results/`).
5. **Enums are open.** `(OrderStatus)42` is legal, so a switch on an enum always needs `_ =>` (CS8524).
6. **Try-pattern errors carry no data.** `AlreadyShipped` cannot say which tracking number the order has.
7. **`default(Result<T,E>)` is an Err with a null error.** It compiles without a nullable warning.
8. **Ship changes its input.** The caller's order object is now in a different state.

## What C# 14 already fixes

`ImmutableRecords/OrderService.cs` has one immutable record per state and errors that carry data. It fixes points
1, 2, 3, 6, and 8 with no new language feature. A `Shipped` always has a `TrackingNumber`, and `Describe` needs no `!`.

One problem is left: **the hierarchy is open.** Any assembly can derive from `abstract record Order`, so every switch
needs `_ => throw new UnreachableException()` (CS8509). Delete a real arm and it still compiles. Add a sixth state and
every switch still compiles. This is the gap that C# 15 closes (`Domain.After/WhatChanged.md`).
