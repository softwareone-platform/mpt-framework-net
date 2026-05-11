using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mpt.Framework.Delta;

public class DeltaJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public sealed override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Delta<>);
    }

    /// <inheritdoc />
    public sealed override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converter = (JsonConverter)Activator.CreateInstance(typeof(DeltaJsonConverter<>).MakeGenericType(typeToConvert.GenericTypeArguments[0]))!;
        return converter;
    }
}
