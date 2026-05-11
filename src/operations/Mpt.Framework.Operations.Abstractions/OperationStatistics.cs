namespace Mpt.Framework.Operations;

public class OperationStatistics
{
    public int Total { get; set; }

    public int Succeeded { get; set; }

    public int Failed { get; set; }

    public int Cancelled { get; set; }

    public bool AllSucceeded() => Total == Succeeded;
}
