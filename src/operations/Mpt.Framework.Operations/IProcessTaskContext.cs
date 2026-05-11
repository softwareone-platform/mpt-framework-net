namespace Mpt.Framework.Operations;

public interface IProcessTaskContext<out TTaskData>
{
    public TaskMetadata Metadata { get; }

    /// <summary>
    /// Task data.
    /// </summary>
    TTaskData Task { get; }
}
