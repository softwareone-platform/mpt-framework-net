using Mpt.Framework.Operations;
using System.Runtime.CompilerServices;

namespace Mpt.Framework.Operations.Tests.Functionality;

public class TestOperation(OperationContext<TestOperation.OperationData> testContext) : Operation<TestOperation.OperationData, TestOperation.TaskData>
{
    public override bool AllMustSucceed => false;

    public override Task OnStartingAsync(IOperationStartingContext<OperationData> context, CancellationToken cancellationToken)
    {
        testContext.StartConditionAttempts++;
        if (testContext.Config.ShoulFailOnStart)
        {
            throw new Exception("Operation failed on start condition.");
        }

        if (testContext.Config.SimulateStartupAttempts > 0 && testContext.Config.SimulateStartupAttempts > context.Attempt)
        {
            context.Postpone(TimeSpan.Zero);
        }

        return base.OnStartingAsync(context, cancellationToken);
    }

    public override async IAsyncEnumerable<TaskData> GetTasksAsync(IOperationPreparingContext<OperationData> context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (testContext.Config.DelayBeforeProduceTasks != TimeSpan.Zero)
        {
            await Task.Delay(testContext.Config.DelayBeforeProduceTasks, cancellationToken);
        }

        for (int i = 0; i < testContext.Config.TotalTasks; i++)
        {
            yield return new TaskData() { };
        }
    }

    public override async Task<TaskResult> ProcessTaskAsync(IProcessTaskContext<TaskData> context, CancellationToken cancellationToken)
    {
        if (testContext.Config.DelayPerTask != TimeSpan.Zero)
        {
            await Task.Delay(testContext.Config.DelayPerTask, cancellationToken);
        }

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
