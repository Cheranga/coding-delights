using System.Text.Json.Serialization;
using OrderProcessorFuncApp.Domain.Http;
using OrderProcessorFuncApp.Domain.Messaging;

namespace OrderProcessorFuncApp.Domain.Models;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(CreateOrderRequestDto))]
[JsonSerializable(typeof(ProcessOrderMessage))]
internal partial class AppJsonContext : JsonSerializerContext { }
