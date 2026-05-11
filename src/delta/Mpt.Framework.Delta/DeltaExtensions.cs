namespace Mpt.Framework.Delta;

public static class DeltaExtensions
{
    public static IEnumerable<Delta<TValue>> Split<TValue>(this IDelta<IEnumerable<TValue>> delta) where TValue : class
    {
        if (delta.Data == default)
        {
            yield break;
        }

        if (delta.Node is not DeltaArrayNode)
        {
            yield break;
        }

        var ix = 0;
        foreach (var dataItem in delta.Data)
        {
            var deltaNode = delta.Node[ix]!;
            deltaNode.SetData(dataItem);
            yield return new Delta<TValue>(deltaNode, $"{delta.Path}[{ix}]");
            ix++;
        }
    }
}
