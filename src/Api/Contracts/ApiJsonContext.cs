using System.Text.Json;

namespace Api.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(OrderResponse))]
[JsonSerializable(typeof(ShipRequest))]
[JsonSerializable(typeof(NotFoundInfo))]
[JsonSerializable(typeof(OrderEvent[]))]
[JsonSerializable(typeof(PaymentMethod))]
public partial class ApiJsonContext : JsonSerializerContext;
