using System.Text.Json.Nodes;

namespace Mpt.Framework.Delta;

public abstract class DeltaNode(string name)
{
    protected readonly Dictionary<string, DeltaNode> _children = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; init; } = name;

    internal object? Data { get; private set; }

    public void SetData(object? data) => Data = data;

    public bool TryGetChild(string propertyName, out DeltaNode? child) => _children.TryGetValue(propertyName, out child);

    public IEnumerable<DeltaNode> Children => _children.Values;

    public DeltaNode this[int index] => _children.ElementAt(index).Value;

    internal DeltaNode Copy()
    {
        DeltaNode node = this switch
        {
            DeltaObjectNode => new DeltaObjectNode(Name),
            DeltaArrayNode => new DeltaArrayNode(Name),
            DeltaValueNode => new DeltaValueNode(Name),
            _ => throw new NotSupportedException($"Unsupported node type: {GetType().Name}"),
        };

        foreach (var child in _children.Values)
        {
            node._children.Add(child.Name, child.Copy());
        }

        return node;
    }

    internal static DeltaNode FromJsonNode(string name, JsonNode json)
    {
        return json switch
        {
            JsonObject jObj => FromJsonObject(name, jObj),
            JsonArray jArr => FromJsonArray(name, jArr),
            _ => new DeltaValueNode(name),
        };
    }

    private static DeltaObjectNode FromJsonObject(string name, JsonObject json)
    {
        var node = new DeltaObjectNode(name);
        foreach (var property in SafeEnumerateProperties(json))
        {
            var propertyName = property.Key;
            if (!node._children.TryAdd(propertyName, FromJsonNode(propertyName, property.Value!)))
            {
                throw new DeltaException($"Duplicate property '{propertyName}' found in JSON object.");
            }
        }

        return node;
    }

    private static List<KeyValuePair<string, JsonNode?>> SafeEnumerateProperties(JsonObject json)
    {
        try
        {
            return [.. json];
        }
        catch (ArgumentException ex)
        {
            throw new DeltaException(ex.Message);
        }
    }

    private static DeltaArrayNode FromJsonArray(string name, JsonArray json)
    {
        var node = new DeltaArrayNode(name);
        for (var i = 0; i < json.Count; i++)
        {
            var child = json[i];
            var propertyName = $"item_{i}";
            node._children.Add(propertyName, FromJsonNode(propertyName, child!));
        }

        return node;
    }
}

internal class DeltaObjectNode : DeltaNode
{
    internal DeltaObjectNode(string name) : base(name) { }
}

internal class DeltaArrayNode : DeltaNode
{
    internal DeltaArrayNode(string name) : base(name) { }
}

internal class DeltaValueNode : DeltaNode
{
    internal DeltaValueNode(string name) : base(name) { }
}
