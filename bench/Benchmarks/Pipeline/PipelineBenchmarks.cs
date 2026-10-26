using BenchmarkDotNet.Attributes;
using static Benchmarks.OrderInputs;
using After = Domain.After;
using Before = Domain.Before;

namespace Benchmarks.Pipeline;

[MemoryDiagnoser]
public class PipelineBenchmarks
{
    [Params(0, 10, 50)]
    public int FailurePercent { get; set; }

    private OrderInput[] _inputs = [];

    [GlobalSetup]
    public void Setup()
    {
        _inputs = Create(FailurePercent);
        foreach (var x in _inputs)
        {
            string expected = Before.TryPattern.OrderService.Handle(LoadBefore(x.Status), x.TrackingNumber);
            string[] actual =
            [
                Before.Exceptions.OrderService.Handle(LoadBefore(x.Status), x.TrackingNumber),
                Before.RecordHierarchy.OrderService.Handle(LoadBefore(x.Status), x.TrackingNumber),
                Before.ResultStruct.OrderService.Handle(LoadBefore(x.Status), x.TrackingNumber),
                Before.ImmutableRecords.OrderService.Handle(LoadImmutable(x.Status), x.TrackingNumber),
                After.ClosedHierarchy.OrderService.Handle(LoadClosed(x.Status), x.TrackingNumber),
                DeclaredUnion.Handle(LoadAfter(x.Status), x.TrackingNumber),
                HandWrittenUnion.Handle(LoadAfter(x.Status), x.TrackingNumber),
                GeneratedUnion.Handle(LoadAfter(x.Status), x.TrackingNumber),
                After.OrderWorkflow.Handle(LoadAfter(x.Status), x.TrackingNumber),
            ];
            if (actual.Any(a => a != expected)) throw new InvalidOperationException($"Variants disagree on {x}.");
        }
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int Exceptions()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += Before.Exceptions.OrderService.Handle(LoadBefore(x.Status), x.TrackingNumber).Length;
        return n;
    }

    [Benchmark(OperationsPerInvoke = Count, Baseline = true)]
    public int TryPattern()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += Before.TryPattern.OrderService.Handle(LoadBefore(x.Status), x.TrackingNumber).Length;
        return n;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int RecordHierarchy()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += Before.RecordHierarchy.OrderService.Handle(LoadBefore(x.Status), x.TrackingNumber).Length;
        return n;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int ResultStruct()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += Before.ResultStruct.OrderService.Handle(LoadBefore(x.Status), x.TrackingNumber).Length;
        return n;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int ImmutableRecords()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += Before.ImmutableRecords.OrderService.Handle(LoadImmutable(x.Status), x.TrackingNumber).Length;
        return n;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int ClosedHierarchy()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += After.ClosedHierarchy.OrderService.Handle(LoadClosed(x.Status), x.TrackingNumber).Length;
        return n;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int UnionDeclared()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += DeclaredUnion.Handle(LoadAfter(x.Status), x.TrackingNumber).Length;
        return n;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int UnionHandWritten()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += HandWrittenUnion.Handle(LoadAfter(x.Status), x.TrackingNumber).Length;
        return n;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int UnionGenerated()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += GeneratedUnion.Handle(LoadAfter(x.Status), x.TrackingNumber).Length;
        return n;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int UnionWorkflow()
    {
        int n = 0;
        foreach (ref readonly var x in _inputs.AsSpan()) n += After.OrderWorkflow.Handle(LoadAfter(x.Status), x.TrackingNumber).Length;
        return n;
    }
}
