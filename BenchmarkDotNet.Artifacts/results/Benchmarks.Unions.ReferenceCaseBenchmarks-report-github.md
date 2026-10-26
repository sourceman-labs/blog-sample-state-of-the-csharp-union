```

BenchmarkDotNet v0.16.0-preview.2, macOS Tahoe 26.7 (25G229) [Darwin 25.6.0]
Apple M2 Pro, 1 CPU, 12 logical and 12 physical cores
Memory: 32 GB Total, 0.36 GB Available
.NET SDK 11.0.100-rc.1.26425.128
  [Host]     : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), Arm64 RyuJIT armv8.0-a


```
| Method         | Mean      | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|-------:|----------:|------------:|
| CreateAsObject | 4.1716 ns | 0.0360 ns | 0.0336 ns |  1.00 | 0.0029 |      24 B |        1.00 |
| CreateAsUnion  | 3.3187 ns | 0.0249 ns | 0.0221 ns |  0.80 | 0.0029 |      24 B |        1.00 |
| CreateAsClosed | 4.1760 ns | 0.0362 ns | 0.0338 ns |  1.00 | 0.0029 |      24 B |        1.00 |
| WrapAsUnion    | 0.8851 ns | 0.0049 ns | 0.0043 ns |  0.21 |      - |         - |        0.00 |
