using BenchmarkDotNet.Attributes;
using UnionGen.Sample;

namespace Benchmarks.Unions;

[MemoryDiagnoser]
public class ValueCaseBenchmarks
{
    private const int N = 1000;
    private readonly string[] _inputs = Enumerable.Range(0, N).Select(Samples.Input).ToArray();

    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public int UnionDeclaration()
    {
        int n = 0;
        foreach (var s in _inputs) n += Samples.Declared(s);
        return n;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int HandWrittenUnion()
    {
        int n = 0;
        foreach (var s in _inputs) n += Samples.HandWritten(s);
        return n;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int GeneratedUnion()
    {
        int n = 0;
        foreach (var s in _inputs) n += Samples.Generated(s);
        return n;
    }
}
