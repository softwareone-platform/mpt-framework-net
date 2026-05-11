using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mpt.Framework.Operation;

internal static class OperationSerializerOptions
{
    public static JsonSerializerOptions Default { get; } = Build();

    public static void ConfigureDefaultOptions(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }

    private static JsonSerializerOptions Build()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ConfigureDefaultOptions(options);
        return options;
    }
}
