using System.Runtime.CompilerServices;
using Domain.After;
using UnionGen.Sample;
using Declared = UnionGen.Sample.Declared;
using Generated = UnionGen.Sample.Generated;
using HandWritten = UnionGen.Sample.HandWritten;

Console.WriteLine($"{"Type",-52} {"Size",5} {"B / 1000 ops",13}");
Report<Declared.ParseResult>("union ParseResult(int, ErrorCode)", static i => Samples.Declared(Samples.Input(i)));
Report<HandWritten.ParseResult>("hand-written [Union] ParseResult", static i => Samples.HandWritten(Samples.Input(i)));
Report<Generated.ParseResult>("[ValueUnion] ParseResult", static i => Samples.Generated(Samples.Input(i)));
Report<Generated.JsonScalar>("[ValueUnion] JsonScalar(string, long, double, bool)", Samples.Scalar);
Report<Result<Shipped, ShipError>>("union Result<Shipped, ShipError>", Samples.DeclaredResult);
Report<Generated.ShipResult>("[ValueUnion] ShipResult(Ok<Shipped>, Err<..>)", Samples.GeneratedResult);

static void Report<T>(string name, Func<int, int> op)
{
    const int WarmUpRuns = 20;
    for (int i = 0; i < WarmUpRuns; i++) Run(op);
    long before = GC.GetAllocatedBytesForCurrentThread();
    Run(op);
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Console.WriteLine($"{name,-52} {Unsafe.SizeOf<T>(),5} {allocated,13:N0}");

    static int Run(Func<int, int> op)
    {
        int total = 0;
        for (int i = 0; i < 1000; i++) total += op(i);
        return total;
    }
}

public static class Samples
{
    private static readonly Shipped Shipped = CreateShipped();
    private static readonly NotPaid NotPaid = new(new OrderId(1));

    public static string Input(int i) => (i % 4) switch { 0 => "42", 1 => "", 2 => "x", _ => "7" };

    // The same parse body three times: only the return type differs.
    public static Declared.ParseResult ParseDeclared(string s) =>
        s.Length == 0 ? ErrorCode.Empty : int.TryParse(s, out var n) ? n : ErrorCode.NotANumber;

    public static HandWritten.ParseResult ParseHandWritten(string s) =>
        s.Length == 0 ? ErrorCode.Empty : int.TryParse(s, out var n) ? n : ErrorCode.NotANumber;

    public static Generated.ParseResult ParseGenerated(string s) =>
        s.Length == 0 ? ErrorCode.Empty : int.TryParse(s, out var n) ? n : ErrorCode.NotANumber;

    public static int Declared(string s) => ParseDeclared(s) switch
    {
        int n => n,
        ErrorCode e => -(int)e,
    };

    public static int HandWritten(string s) => ParseHandWritten(s) switch
    {
        int n => n,
        ErrorCode e => -(int)e,
    };

    public static int Generated(string s) => ParseGenerated(s) switch
    {
        int n => n,
        ErrorCode e => -(int)e,
    };

#if DEMO_MISSING_ARM
    public static int Missing(Generated.ParseResult r) => r switch { int n => n };
#endif

    internal static int Scalar(int i)
    {
        Generated.JsonScalar scalar = (i % 4) switch { 0 => "text", 1 => (long)i, 2 => i * 0.5, _ => true };
        return scalar switch
        {
            string s => s.Length,
            long l => (int)l,
            double d => (int)d,
            bool b => b ? 1 : 0,
        };
    }

    internal static int DeclaredResult(int i)
    {
        Result<Shipped, ShipError> result = i % 2 == 0 ? Result.Ok(Shipped) : Result.Err<ShipError>(NotPaid);
        return result switch { Ok<Shipped> => 1, Err<ShipError> => 0 };
    }

    internal static int GeneratedResult(int i)
    {
        Generated.ShipResult result = i % 2 == 0 ? Result.Ok(Shipped) : Result.Err<ShipError>(NotPaid);
        return result switch { Ok<Shipped> => 1, Err<ShipError> => 0 };
    }

    private static Shipped CreateShipped() =>
        TrackingNumber.TryParse("TRACK-1", out var trackingNumber)
            ? new Shipped(new PaymentId("PAY-1"), trackingNumber)
            : throw new InvalidOperationException("The sample input is valid.");
}
