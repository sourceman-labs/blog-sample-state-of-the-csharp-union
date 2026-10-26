using BenchmarkDotNet.Attributes;

namespace Benchmarks.Unions;

internal sealed record Circle(double Radius);
internal sealed record Square(double Side);

internal union Shape(Circle, Square);

internal closed record ClosedShape;
internal sealed record ClosedCircle(double Radius) : ClosedShape;
internal sealed record ClosedSquare(double Side) : ClosedShape;

[MemoryDiagnoser]
public class ReferenceCaseBenchmarks
{
    private const int N = 1000;
    private static readonly Circle CachedCircle = new(1);
    private static readonly Square CachedSquare = new(2);

    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public double CreateAsObject()
    {
        double total = 0;
        for (int i = 0; i < N; i++)
        {
            object shape = (i & 1) == 0 ? new Circle(i) : new Square(i);
            total += shape switch { Circle c => c.Radius, Square s => s.Side, _ => 0 };
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public double CreateAsUnion()
    {
        double total = 0;
        for (int i = 0; i < N; i++)
        {
            Shape shape = (i & 1) == 0 ? new Circle(i) : new Square(i);
            total += shape switch { Circle c => c.Radius, Square s => s.Side };
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public double CreateAsClosed()
    {
        double total = 0;
        for (int i = 0; i < N; i++)
        {
            ClosedShape shape = (i & 1) == 0 ? new ClosedCircle(i) : new ClosedSquare(i);
            total += shape switch { ClosedCircle c => c.Radius, ClosedSquare s => s.Side };
        }
        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public double WrapAsUnion()
    {
        double total = 0;
        for (int i = 0; i < N; i++)
        {
            Shape shape = (i & 1) == 0 ? CachedCircle : CachedSquare;
            total += shape switch { Circle c => c.Radius, Square s => s.Side };
        }
        return total;
    }
}
