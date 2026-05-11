namespace Mpt.Framework.Delta;

public interface IDelta<out TData>
{
    DeltaNode? Node { get; }

    TData? Data { get; }

    bool IsDefined { get; }

    string Path { get; }
}
