using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mpt.Framework.Delta;

public static class DeltaBuilder
{
    public static Delta<TData> Empty<TData>() => new Delta<TData>(null, null);

    public static Delta<TData> FromObject<TData>(object input, JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize(input);
        return FromJson<TData>(json, options);
    }

    public static Delta<TData> FromJson<TData>(string json, JsonSerializerOptions? options = null)
    {
        using var document = JsonDocument.Parse(json);
        return FromDocument<TData>(document, options);
    }

    public static Delta<TData> FromReader<TData>(ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return FromDocument<TData>(document, options);
    }

    private static Delta<TData> FromDocument<TData>(JsonDocument document, JsonSerializerOptions? options = null)
    {
        var root = JsonObject.Create(document!.RootElement);
        var deltaNode = DeltaNode.FromJsonNode("root", root!);
        deltaNode.SetData(root.Deserialize<TData>(options));
        return new Delta<TData>(deltaNode);
    }
}
