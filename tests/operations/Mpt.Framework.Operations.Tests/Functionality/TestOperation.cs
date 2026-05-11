using System.Runtime.CompilerServices;

namespace Mpt.Framework.Operations.Tests.Functionality;

/// <summary>
/// A scriptable operation whose lifecycle hooks branch on <see cref="OperationContext{TOperation}.Config"/>
/// flags. Lets each test set up a specific scenario (failures on start, errors while preparing tasks,
/// all-must-succeed semantics, etc.) without writing a new operation type per case.
/// </summary>
public class TestOperation(OperationContext<TestOperation.OperationData> testContext) : Operation<TestOperation.OperationData, TestOperation.TaskData>
{
    public override bool AllMustSucceed => testContext.Config.AllMustSucceed;

    public override Task OnStartingAsync(IOperationStartingContext<OperationData> context, CancellationToken cancellationToken)
    {
        testContext.StartConditionAttempts++;
        if (testContext.Config.ShouldFailOnStart)
            throw new InvalidOperationException("Operation failed on start condition.");

        if (testContext.Config.SimulateStartupAttempts > 0 && testContext.Config.SimulateStartupAttempts > context.Attempt)
            context.Postpone(TimeSpan.Zero);

        return base.OnStartingAsync(context, cancellationToken);
    }

    public override async IAsyncEnumerable<TaskData> GetTasksAsync(IOperationPreparingContext<OperationData> context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (testContext.Config.DelayBeforeProduceTasks != TimeSpan.Zero)
            await Task.Delay(testContext.Config.DelayBeforeProduceTasks, cancellationToken);

        if (testContext.Config.ShouldThrowInGetTasks)
            throw new InvalidOperationException("Operation failed while preparing tasks.");

        for (int i = 0; i < testContext.Config.TotalTasks; i++)
            yield return new TaskData();
    }

    public override async Task<TaskResult> ProcessTaskAsync(IProcessTaskContext<TaskData> context, CancellationToken cancellationToken)
    {
        if (testContext.Config.DelayPerTask != TimeSpan.Zero)
            await Task.Delay(testContext.Config.DelayPerTask, cancellationToken);

        // Fails the first N tasks (N = ShouldFailTask), then succeeds. Relies on concurrency = 1
        // to be deterministic; OperationContext registers the operation with Tasks.Concurrency = 1.
        var success = testContext.Failed >= testContext.Config.ShouldFailTask;
        testContext.ReportTaskComplete(success);
        return success ? TaskResult.Success : TaskResult.Failure;
    }

    public override Task OnFinishedAsync(IOperationFinishedContext<OperationData> context, CancellationToken cancellationToken)
    {
        testContext.ReportOperationComplete(context.Result);
        return base.OnFinishedAsync(context, cancellationToken);
    }

    public class OperationData : IOperationContract
    {
    }

    public class TaskData
    {
    }
}
