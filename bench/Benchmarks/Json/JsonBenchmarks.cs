using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Benchmarks.Json;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
public class JsonBenchmarks
{
    private static readonly PaymentJsonContext Ctx = PaymentJsonContext.Default;

    private readonly PaymentUnion[] _unions = [new Card("4242424242424242", 2030), new BankTransfer("SE4550000000058398257466")];
    private readonly PaymentClosed[] _closed = [new CardPayment("4242424242424242", 2030), new BankPayment("SE4550000000058398257466")];
    private readonly PaymentDto[] _dtos = [new() { Number = "4242424242424242", ExpiryYear = 2030 }, new() { Iban = "SE4550000000058398257466" }];

    private byte[][] _unionJson = [], _closedJson = [], _dtoJson = [];

    [GlobalSetup]
    public void Setup()
    {
        _unionJson = [.. _unions.Select(u => JsonSerializer.SerializeToUtf8Bytes(u, Ctx.PaymentUnion))];
        _closedJson = [.. _closed.Select(c => JsonSerializer.SerializeToUtf8Bytes(c, Ctx.PaymentClosed))];
        _dtoJson = [.. _dtos.Select(d => JsonSerializer.SerializeToUtf8Bytes(d, Ctx.PaymentDto))];
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public int SerializeDto()
    {
        int n = 0;
        foreach (var d in _dtos) n += JsonSerializer.SerializeToUtf8Bytes(d, Ctx.PaymentDto).Length;
        return n;
    }

    [BenchmarkCategory("Serialize"), Benchmark]
    public int SerializeUnion()
    {
        int n = 0;
        foreach (var u in _unions) n += JsonSerializer.SerializeToUtf8Bytes(u, Ctx.PaymentUnion).Length;
        return n;
    }

    [BenchmarkCategory("Serialize"), Benchmark]
    public int SerializeClosed()
    {
        int n = 0;
        foreach (var c in _closed) n += JsonSerializer.SerializeToUtf8Bytes(c, Ctx.PaymentClosed).Length;
        return n;
    }

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public int DeserializeDto()
    {
        int n = 0;
        foreach (var json in _dtoJson)
        {
            var dto = JsonSerializer.Deserialize(json, Ctx.PaymentDto)!;
            n += dto.Iban is { } iban ? iban.Length : dto.ExpiryYear ?? 0;
        }
        return n;
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int DeserializeUnion()
    {
        int n = 0;
        foreach (var json in _unionJson)
        {
            n += JsonSerializer.Deserialize(json, Ctx.PaymentUnion) switch
            {
                Card c => c.ExpiryYear,
                BankTransfer b => b.Iban.Length,
            };
        }
        return n;
    }

    [BenchmarkCategory("Deserialize"), Benchmark]
    public int DeserializeClosed()
    {
        int n = 0;
        foreach (var json in _closedJson)
        {
            n += JsonSerializer.Deserialize(json, Ctx.PaymentClosed)! switch
            {
                CardPayment c => c.ExpiryYear,
                BankPayment b => b.Iban.Length,
            };
        }
        return n;
    }
}
