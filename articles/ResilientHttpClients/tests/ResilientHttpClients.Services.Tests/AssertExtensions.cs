using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResilientHttpClients.Services.Tests;

internal static class AssertExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static bool AreSame<T>(T expected, T actual, JsonSerializerOptions options)
    {
        var expectedJson = JsonSerializer.Serialize(expected, options);
        var actualJson = JsonSerializer.Serialize(actual, options);

        return expectedJson == actualJson;
    }

    public static bool AreSame<T>(T expected, T actual) => AreSame(expected, actual, DefaultOptions);
}
