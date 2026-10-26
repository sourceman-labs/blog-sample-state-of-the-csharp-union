using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benchmarks.Json;

internal sealed record Card(string Number, int ExpiryYear);
internal sealed record BankTransfer(string Iban);

[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
internal union PaymentUnion(Card, BankTransfer);

[JsonPolymorphic(InferClosedTypePolymorphism = true)]
internal closed record PaymentClosed;
internal sealed record CardPayment(string Number, int ExpiryYear) : PaymentClosed;
internal sealed record BankPayment(string Iban) : PaymentClosed;

internal sealed class PaymentDto
{
    public string? Number { get; init; }
    public int? ExpiryYear { get; init; }
    public string? Iban { get; init; }
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
    RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PaymentUnion))]
[JsonSerializable(typeof(PaymentClosed))]
[JsonSerializable(typeof(PaymentDto))]
internal partial class PaymentJsonContext : JsonSerializerContext;
