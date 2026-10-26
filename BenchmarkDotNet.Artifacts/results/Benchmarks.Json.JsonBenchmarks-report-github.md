```

BenchmarkDotNet v0.16.0-preview.2, macOS Tahoe 26.7 (25G229) [Darwin 25.6.0]
Apple M2 Pro, 1 CPU, 12 logical and 12 physical cores
Memory: 32 GB Total, 0.32 GB Available
.NET SDK 11.0.100-rc.1.26425.128
  [Host]     : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), Arm64 RyuJIT armv8.0-a


```
| Method            | Categories  | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------ |------------ |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| DeserializeDto    | Deserialize | 217.4 ns |  2.79 ns |  2.33 ns |  1.00 |    0.00 | 0.0248 |     208 B |        1.00 |
| DeserializeUnion  | Deserialize | 803.4 ns | 14.32 ns | 13.40 ns |  3.70 |    0.07 | 0.1659 |    1392 B |        6.69 |
| DeserializeClosed | Deserialize | 512.3 ns |  3.86 ns |  3.42 ns |  2.36 |    0.03 | 0.0648 |     544 B |        2.62 |
|                   |             |          |          |          |       |         |        |           |             |
| SerializeDto      | Serialize   | 121.3 ns |  1.60 ns |  1.42 ns |  1.00 |    0.00 | 0.0162 |     136 B |        1.00 |
| SerializeUnion    | Serialize   | 185.7 ns |  0.71 ns |  0.63 ns |  1.53 |    0.02 | 0.0908 |     760 B |        5.59 |
| SerializeClosed   | Serialize   | 248.8 ns |  2.61 ns |  2.44 ns |  2.05 |    0.03 | 0.0963 |     808 B |        5.94 |
