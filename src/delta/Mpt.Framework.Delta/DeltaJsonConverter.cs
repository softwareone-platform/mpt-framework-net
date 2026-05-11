using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mpt.Framework.Delta;

public class DeltaJsonConverter<T> : JsonConverter<Delta<T>>
{
    public override Delta<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DeltaBuilder.FromReader<T>(ref reader, DeltaJsonSerializerOptions.Options);
    }

    public override void Write(Utf8JsonWriter writer, Delta<T> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Write method is not implemented for delta");
    }
}
