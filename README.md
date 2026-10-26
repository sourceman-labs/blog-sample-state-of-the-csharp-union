# State of the C# union: sample code

Sample code for the blog post [State of the csharp union](https://www.sourceman.se/blog/state-of-the-csharp-union/).

It models an order's state and its failures on .NET 11 (C# 15 `union`). It compares exceptions, try-patterns, record hierarchies, `closed` hierarchies, and `union` types. It also shows non-boxing unions, both hand-written and source-generated.

## Layout

- `src/Domain.Before`: C# 14 variants.
- `src/Domain.After`: C# 15 `union` and `closed` variants.
- `src/UnionGen`: source generator for `[ValueUnion]`.
- `src/UnionGen.Sample`: generator usage and allocation checks.
- `src/Api` and `src/Client`: minimal API and its client.
- `bench/Benchmarks`: BenchmarkDotNet benchmarks.
- `tests/Tests`: tests.

## Run

You need the .NET 11 SDK. `global.json` pins `11.0.100-rc.1`.

```sh
dotnet test tests/Tests
dotnet run --project src/Client
dotnet run -c Release --project bench/Benchmarks -- --filter '*'
```

Close other apps first, because the results depend on free memory. Earlier reports are in `BenchmarkDotNet.Artifacts/results/`.
