# The allocation story

All claims are checked against .NET 11 RC1. Numbers from `dotnet run -c Release --project src/UnionGen.Sample`
(`GC.GetAllocatedBytesForCurrentThread`, 1000 create + match, after warm-up). BenchmarkDotNet numbers are in `BenchmarkDotNet.Artifacts/results/`.

## Files

| Concept | File |
|---|---|
| Boxing baseline | `Declared/ParseResult.cs`: `union ParseResult(int, ErrorCode);` |
| Hand-written non-boxing union | `HandWritten/ParseResult.cs` (28 lines, 21 non-blank/non-comment) |
| Generated, unmanaged cases | `Generated/ParseResult.cs` |
| Generated, reference + unmanaged | `Generated/JsonScalar.cs` |
| Generated, managed structs | `Generated/ShipResult.cs` (`Ok<Shipped>`, `Err<ShipError>`) |
| Generator | `src/UnionGen/ValueUnionGenerator.cs` (~170 lines + 35 lines of model) |
| Generated output | `obj/<config>/net11.0/generated/UnionGen/UnionGen.ValueUnionGenerator/*.g.cs` |
| Tests | `tests/Tests/UnionGenTests.cs` |

## Results

| Type | Size | B / 1000 ops |
|---|---:|---:|
| `union ParseResult(int, ErrorCode)` | 8 | **24 000** |
| hand-written `[Union]` `ParseResult` | 8 | **0** |
| `[ValueUnion]` `ParseResult` | 8 | **0** |
| `[ValueUnion]` `JsonScalar(string, long, double, bool)` | 24 | **0** |
| `union Result<Shipped, ShipError>` | 8 | **24 000** |
| `[ValueUnion]` `ShipResult(Ok<Shipped>, Err<ShipError>)` | 24 | **0** |

The `Shipped` and `NotPaid` instances are cached, so the table shows the union cost only.
`Result<,>` boxes even though every payload is a class: `Ok<T>`/`Err<E>` are structs, and `Value` is `object?`.

## How the compiler finds the non-boxing path

- `[Union]` + `IUnion` + one public single-parameter ctor per case → implicit conversions and exhaustive `switch`.
- Add `bool HasValue` + `bool TryGetValue(out TCase)` per case → pattern matching calls `TryGetValue`.
  Proof: a probe whose `Value` getter throws still matches correctly. `Value` is never called by `switch`/`is`.
- Delete an arm → **CS8509** (as error). Toggle: `-p:DefineConstants=DEMO_MISSING_ARM` on `UnionGen.Sample`.
- Nested patterns work: `Err<ShipError>(BadTrackingNumber b)` on the generated `ShipResult`.
- `[Union]` on a generated `partial` part is enough. The user's part needs no union attribute.

## Storage strategies

| Case kind | Strategy | Why |
|---|---|---|
| Reference type (`string`, records) | One shared `object? _ref` + cast on read | Only one case is active. One slot covers all of them. |
| Unmanaged (`int`, `long`, `double`, enums, `bool`) | One `[StructLayout(Explicit)]` blob, every case at `FieldOffset(0)` | Size = largest case, not the sum |
| Managed struct (`Ok<Shipped>`, `Err<ShipError>`, any struct with a reference inside) | Own field per case | The GC must know what is a pointer. An `object` overlapped by a `long` compiles, then fails at type load (`TypeLoadException`) |
| Tag | `byte`, `0` = `default(T)` | `HasValue == false` for `default` |

Trade-offs:

- **Size vs boxing.** The `union` declaration is always 8 bytes (one reference) but boxes every value-type case.
  The non-boxing struct is `tag + max(unmanaged) + 8 per reference slot + sum(managed structs)`, rounded up for alignment.
  `ParseResult` 8 B, `JsonScalar` 24 B, `ShipResult` 24 B.
- **Copying cost.** A struct union is copied on every pass-by-value, return, and `switch`. 24 B is cheap.
  A union with a few `Guid`/`decimal` cases or several managed structs grows fast; at that point either pass by `in`,
  or accept the box (one allocation, then 8-byte copies).
- **Managed structs do not share.** `ShipResult` has one field for `Ok<Shipped>` and one for `Err<ShipError>`,
  even though both hold just one reference. A smarter generator could unwrap single-reference structs into `_ref`,
  but that needs `Unsafe.As` tricks and is not worth it in a PoC.
- **`Value` still boxes.** It is required by the pattern. Only code that asks for it (`IUnion`, logging, STJ) pays.
  Easy bug: for `(int, long)` a `switch` expression in `Value` gets the natural type `long` and boxes an `int` as `long`.
  The generator casts every arm to `object`, so `Value` is an `Int32`.
- **The `default` hole stays.** `default(ParseResult)` has tag 0: `HasValue` is false, `switch` throws
  `SwitchExpressionException`, `Match` throws `InvalidOperationException`. Same as a `union` declaration.

## The generator

```csharp
[ValueUnion, UnionCase<int>, UnionCase<ErrorCode>]
public readonly partial struct ParseResult;
```

- `IIncrementalGenerator` + `ForAttributeWithMetadataName("UnionGen.ValueUnionAttribute")`.
  The attributes come from `RegisterPostInitializationOutput`, so the consumer needs no extra package.
- The model is a record with an `EquatableArray<T>`. Symbols never leave the transform step, so caching works.
- It emits ctors, `Value`, `HasValue`, one `TryGetValue` per case, and `Match<TResult>(Func<...>...)`.
  With `static` lambdas, `Match` allocates nothing either (the runtime caches non-capturing delegates).
- Reference-type ctors throw `ArgumentNullException`, so `[NotNullWhen(true)]` on `TryGetValue` is honest.

PoC limits (a real generator would report diagnostics instead):

- **Non-generic only.** `[UnionCase<T>]` on `partial struct Result<T, E>` is **CS8968** ("an attribute type argument cannot
  use type parameters"). `union Result<T, E>(Ok<T>, Err<E>)` has no such limit. A generic version needs
  `typeof(Ok<>)`-style attributes and type-parameter mapping. That is a lot of generator for one type.
- Top-level types only (no nesting). Duplicate case types fail as duplicate ctors (CS0111), not as a clean diagnostic.

## Analyzer angle

- Roslyn `Microsoft.CodeAnalysis.CSharp` **5.9.0** (latest on NuGet) has public `ITypeSymbol.IsUnion`,
  `Conversion.IsUnion`, `SyntaxKind.UnionKeyword`, and `UnionDeclarationSyntax`.
- **`ITypeSymbol.UnionCaseTypes` is not public in 5.9.0** (only internal `UnionCaseTypesNoUseSiteDiagnostics`).
  An analyzer today can detect a union but must rebuild the case list itself
  (public single-parameter ctors). `IsUnion` is `true` for both a `union` declaration and a custom `[Union]` struct.
- Analyzer idea for the blog: "this `union` has value-type cases → it boxes, consider `[ValueUnion]`".

## Is the generator worth it?

| | Hand-written | Generated |
|---|---|---|
| Lines per union | ~21 code lines | 2 |
| One-time cost | none | ~200 lines generator, netstandard2.0, Roslyn version pinning (≤ host compiler) |
| Risk | Copy-paste bugs: wrong tag in one `TryGetValue` compiles fine | Generator bugs hit every union (see the `Value` natural-type bug above) |
| Generic unions | yes | no (CS8968) |

Verdict: for **one or two** hot-path unions, write the ~20 lines by hand and keep a test like
`Declared_hand_written_and_generated_agree`. Use a generator when a code base has **many** value-type unions. Even then,
this is a gap the platform should close: either `union` declarations get a non-boxing layout, or a well-known
library ships this generator.

When the *cases* are classes (`union ShipError(NotPaid, AlreadyShipped, OrderCancelled, BadTrackingNumber)`), the `union` keyword is
fine as is: the records allocate anyway, and the union adds nothing on top. But `Result<T, E>(Ok<T>, Err<E>)` has
struct cases, so it boxes once per value (24 B) even with class payloads. A `Result` on a hot path is the
strongest case for the non-boxing pattern, and the one the attribute generator cannot express generically.
