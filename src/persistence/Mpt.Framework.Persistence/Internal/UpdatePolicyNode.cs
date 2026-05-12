using System.Linq.Expressions;

namespace Mpt.Framework.Persistence.Internal;

/// <summary>
/// Internal node in the update-policy tree. Each entity gets a root node, and nested
/// properties live as named children. Rules attach to the node corresponding to the
/// property they govern.
/// </summary>
internal class UpdatePolicyNode(string name)
{
    private readonly Dictionary<string, UpdatePolicyNode> _children = new(StringComparer.Ordinal);

    static UpdatePolicyNode()
    {
        Empty = new UpdatePolicyNode(string.Empty);
    }

    public static UpdatePolicyNode Empty { get; }

    public string Name { get; } = name;

    public UpdatePolicyNode? Parent { get; private set; }

    public IReadOnlyDictionary<string, UpdatePolicyNode> Children => _children;

    public List<UpdatePolicyRule> Rules { get; } = [];

    public void AddChild(UpdatePolicyNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        child.Parent = this;
        _children[child.Name] = child;
    }

    public bool TryGetChild(string childName, out UpdatePolicyNode? child)
        => _children.TryGetValue(childName, out child);

    /// <summary>
    /// Walk down the member-access chain in <paramref name="expression"/>, creating
    /// nodes as needed, and return the leaf node.
    /// </summary>
    public UpdatePolicyNode Extend(LambdaExpression expression)
    {
        var segments = ToSegments(expression);
        var currentNode = this;

        while (segments.Count > 0)
        {
            var segment = segments.Pop();

            if (!currentNode.TryGetChild(segment, out var childNode))
            {
                childNode = new UpdatePolicyNode(segment);
                currentNode.AddChild(childNode);
            }

            currentNode = childNode!;
        }

        return currentNode;
    }

    private static Stack<string> ToSegments(LambdaExpression expression)
    {
        var segments = new Stack<string>();
        var currentExpression = expression.Body;

        while (currentExpression is MemberExpression memberExpression)
        {
            segments.Push(memberExpression.Member.Name);
            currentExpression = memberExpression.Expression!;
        }

        if (segments.Count == 0)
        {
            throw new InvalidOperationException(
                $"Unsupported expression type: {expression.Body.GetType().Name}. Only member-access expressions are supported.");
        }

        return segments;
    }
}
