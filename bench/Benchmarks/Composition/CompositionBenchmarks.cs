using BenchmarkDotNet.Attributes;
using Domain.After;
using Domain.After.Composition;
using static Benchmarks.OrderInputs;

namespace Benchmarks.Composition;

[MemoryDiagnoser]
public class CompositionBenchmarks
{
    [Params(0, 10, 50)]
    public int FailurePercent { get; set; }

    private OrderInput[] _inputs = [];

    [GlobalSetup]
    public void Setup() => _inputs = Create(FailurePercent);

    [Benchmark(OperationsPerInvoke = Count, Baseline = true)]
    public int EarlyReturn()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += OrderWorkflow.Handle(LoadAfter(x.Status), x.TrackingNumber).Length;
        return n;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Bind()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += BindStyle.Handle(LoadAfter(x.Status), x.TrackingNumber).Length;
        return n;
    }
}
