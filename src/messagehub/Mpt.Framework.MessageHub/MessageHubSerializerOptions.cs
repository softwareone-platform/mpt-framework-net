using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mpt.Framework.MessageHub;

internal static class MessageHubSerializerOptions
{
    public static void ConfigureDefaultOptions(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }
}
