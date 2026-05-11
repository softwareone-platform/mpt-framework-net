namespace Mpt.Framework.Operation;

public class OperationFailure
{
    public OperationFailureType Type { get; set; }
    public string Message { get; set; } = null!;
}

public enum OperationFailureType
{
    Unknown,
    NoTasks,
    TaskFailedWhileNotAllowed,
    ErrorPreparingTasks,
    ErrorCheckingCondition
}
