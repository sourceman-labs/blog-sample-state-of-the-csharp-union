```

BenchmarkDotNet v0.16.0-preview.2, macOS Tahoe 26.7 (25G229) [Darwin 25.6.0]
Apple M2 Pro, 1 CPU, 12 logical and 12 physical cores
Memory: 32 GB Total, 0.36 GB Available
.NET SDK 11.0.100-rc.1.26425.128
  [Host]     : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), Arm64 RyuJIT armv8.0-a


```
| Method           | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| UnionDeclaration | 7.435 ns | 0.0422 ns | 0.0395 ns |  1.00 | 0.0029 |      24 B |        1.00 |
| HandWrittenUnion | 4.497 ns | 0.0306 ns | 0.0286 ns |  0.60 |      - |         - |        0.00 |
| GeneratedUnion   | 4.499 ns | 0.0334 ns | 0.0312 ns |  0.61 |      - |         - |        0.00 |
