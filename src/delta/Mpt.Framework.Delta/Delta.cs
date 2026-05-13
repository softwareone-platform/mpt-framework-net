using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mpt.Framework.Delta;

public class Delta<TData> : IDelta<TData>
{
    // cache is intended to be partitioned by TData
    private static readonly ConcurrentDictionary<string, Delegate> _accessorCache = new();
    private static readonly ConcurrentDictionary<MemberInfo, string> _nameCache = new();

    public Delta(DeltaNode? node, string? path = null)
    {
        Node = node;
        Path = path ?? string.Empty;
    }

    public DeltaNode? Node { get; init; }

    public string Path { get; init; }

    public TData? Data => Node?.Data != null ? (TData)Node.Data : default;

    public bool IsDefined => Node != null;

    public static implicit operator TData(Delta<TData> delta) => delta.Data!;

    public bool TryGet<TValue>(Expression<Func<TData, TValue>> expression, out TValue child)
    {
        child = default!;

        if (TryGetDelta(expression, out var delta))
        {
            child = delta;
            return true;
        }

        return false;
    }

    public bool TryGetDelta<TValue>(Expression<Func<TData, TValue>> expression, out Delta<TValue> child)
    {
        child = GetDelta(expression);
        return child.IsDefined;
    }

    public bool AssignIfDefined<TValue>(Expression<Func<TData, TValue>> expression, Action<TValue> setter)
    {
        if (TryGet(expression, out var value))
        {
            setter(value);
            return true;
        }

        return false;
    }

    public Delta<TValue> GetDelta<TValue>(Expression<Func<TData, TValue>> expression)
    {
        DeltaNode? tail = Node;

        var pathSb = new StringBuilder();
        if (!string.IsNullOrEmpty(Path))
        {
            AddPathSegment(pathSb, Path);
        }

        foreach (var pathSegment in GetMemberPathSegments(expression))
        {
            tail?.TryGetChild(pathSegment, out tail);
            AddPathSegment(pathSb, pathSegment);
        }

        pathSb.Length--;

        var delta = new Delta<TValue>(tail, pathSb.ToString());

        if (delta.IsDefined && delta.Node!.Data == default)
        {
            var accessor = (Func<TData, TValue>)_accessorCache.GetOrAdd(delta.Path, _ => expression.Compile());
            delta.Node.SetData(accessor(Data!));
        }

        return delta;

        static void AddPathSegment(StringBuilder pathSb, string segment)
        {
            pathSb.Append(segment);
            pathSb.Append('.');
        }
    }

    public Delta<TTarget> MapTo<TTarget>()
    {
        if (!IsDefined)
        {
            return new Delta<TTarget>(null);
        }

        var mappedNode = Node!.Copy();
        if (Data is not null)
        {
            var json = JsonSerializer.Serialize(Data, DeltaJsonSerializerOptions.Options);
            var targetData = JsonSerializer.Deserialize<TTarget>(json, DeltaJsonSerializerOptions.Options);

            if (targetData is not null)
            {
                mappedNode.SetData(targetData);
            }
        }

        return new Delta<TTarget>(mappedNode, Path);
    }

    private static IEnumerable<string> GetMemberPathSegments(LambdaExpression expression)
    {
        var member = TryCastToMemberExpression(expression.Body);

        if (IsNotHead(member))
        {
            var chain = new Stack<MemberExpression>();
            chain.Push(member);

            while (IsNotHead(member))
            {
                member = TryCastToMemberExpression(member.Expression!);
                chain.Push(member);
            }

            while (chain.Count > 0)
            {
                var nextMember = chain.Pop();
                yield return GetMemberName(nextMember.Member);
            }
        }
        else
        {
            yield return GetMemberName(member.Member);
        }
    }

    private static string GetMemberName(MemberInfo member)
    {
        return _nameCache.GetOrAdd(member, static mbr =>
        {
            var nameAttribute = mbr.GetCustomAttribute<JsonPropertyNameAttribute>(true);
            return nameAttribute?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(mbr.Name);
        });
    }

    private static MemberExpression TryCastToMemberExpression(Expression expression)
    {
        if (expression is not MemberExpression member)
        {
            throw new NotImplementedException("Only member expressions are supported in path");
        }

        return member;
    }

    private static bool IsNotHead(MemberExpression member) => member.Expression is not ParameterExpression;
}

public static class DeltaJsonSerializerOptions
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
