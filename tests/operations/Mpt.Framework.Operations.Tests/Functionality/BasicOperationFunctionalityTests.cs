using FluentAssertions;

namespace Mpt.Framework.Operations.Tests.Functionality;

public class BasicOperationFunctionalityTests
{
    [Theory]
    [InlineData(7, 0)]
    [InlineData(9, 3)]
    [InlineData(3, 3)]
    [InlineData(120, 56)]
    public async Task Operation_WhenDispatched_ReportsTotalSucceededFailedCorrectly(int total, int shouldFail)
    {
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = total;
            t.ShouldFailTask = shouldFail;
        });

        await context.StartAsync();
        await context.WaitForCompletion(3000);

        context.Result!.Status.Should().Be(OperationStatus.Succeeded);
        context.Result.Statistics.Should().NotBeNull();
        context.Result.Statistics!.Total.Should().Be(total);

        context.Result.Statistics.Succeeded.Should().Be(total - shouldFail);
        context.Result.Statistics.Succeeded.Should().Be(context.Succeeded);

        context.Result.Statistics.Failed.Should().Be(shouldFail);
        context.Result.Statistics.Failed.Should().Be(context.Failed);

        context.StartConditionAttempts.Should().Be(1, "the operation should not run multiple startup attempts");
    }

    [Fact]
    public async Task Operation_WhenCancelled_ReportsRemainingTasksAsCancelled()
    {
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = 5;
            t.DelayPerTask = TimeSpan.FromMilliseconds(1000);
        });

        await context.StartAsync();
        await Task.Delay(2000);
        await context.CancelAsync();
        await context.WaitForCompletion(5000);

        context.Result!.Status.Should().Be(OperationStatus.Cancelled);
        context.Result.Statistics!.Total.Should().Be(5);
        context.Result.Statistics.Failed.Should().Be(0, "no failures were configured");
        // Sum invariant is structural (Cancelled is computed as Total - Failed - Succeeded), so the
        // assertion that actually proves cancellation worked is: at least one task was cut short.
        context.Result.Statistics.Cancelled.Should().BeGreaterThan(0, "cancel should have cut off the run");
    }

    [Fact]
    public async Task Operation_WhenThrowsOnStart_FailsWithErrorCheckingCondition()
    {
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = 5;
            t.ShouldFailOnStart = true;
        });

        await context.StartAsync();
        await context.WaitForCompletion(5000);

        context.Result.Should().NotBeNull();
        context.Result!.Status.Should().Be(OperationStatus.Failed);
        context.Result.Failure!.Type.Should().Be(OperationFailureType.ErrorCheckingCondition);
        context.StartConditionAttempts.Should().Be(1, "the operation should not retry a synchronous startup failure");
    }

    [Fact]
    public async Task Operation_WhenStartPostponed_CallsOnStartingUntilSatisfied()
    {
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = 5;
            t.SimulateStartupAttempts = 5;
        });

        await context.StartAsync();
        await context.WaitForCompletion(5000);

        context.Result.Should().NotBeNull();
        context.Result!.Status.Should().Be(OperationStatus.Succeeded);
        context.Result.Failure.Should().BeNull();
        context.StartConditionAttempts.Should().Be(5, "the operation must take 5 attempts to satisfy the start condition");
    }

    [Fact]
    public async Task Operation_WhenProducesNoTasks_FailsWithNoTasks()
    {
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = 0;
        });

        await context.StartAsync();
        await context.WaitForCompletion(3000);

        context.Result!.Status.Should().Be(OperationStatus.Failed);
        context.Result.Failure!.Type.Should().Be(OperationFailureType.NoTasks);
    }

    [Fact]
    public async Task Operation_WhenThrowsWhilePreparingTasks_FailsWithErrorPreparingTasks()
    {
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = 5;
            t.ShouldThrowInGetTasks = true;
        });

        await context.StartAsync();
        await context.WaitForCompletion(3000);

        context.Result!.Status.Should().Be(OperationStatus.Failed);
        context.Result.Failure!.Type.Should().Be(OperationFailureType.ErrorPreparingTasks);
    }

    [Fact]
    public async Task Operation_WhenAllMustSucceedAndOneTaskFails_FailsWithTaskFailedWhileNotAllowed()
    {
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = 5;
            t.ShouldFailTask = 1;       // first task fails
            t.AllMustSucceed = true;
        });

        await context.StartAsync();
        await context.WaitForCompletion(3000);

        context.Result!.Status.Should().Be(OperationStatus.Failed);
        context.Result.Failure!.Type.Should().Be(OperationFailureType.TaskFailedWhileNotAllowed);
    }
}
