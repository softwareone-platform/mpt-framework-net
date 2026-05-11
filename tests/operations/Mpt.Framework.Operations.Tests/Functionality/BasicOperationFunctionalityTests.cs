using FluentAssertions;

namespace Mpt.Framework.Operations.Tests.Functionality;

public class BasicOperationFunctionalityTests
{
    [Theory]
    [InlineData(7, 0)]
    [InlineData(9, 3)]
    [InlineData(3, 3)]
    [InlineData(120, 56)]
    public async Task Operation_WhenDispatched_ShouldCompleteSuccessfully(int total, int shouldFail)
    {
        // Arrange
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = total;
            t.ShouldFailTask = shouldFail;
        });

        // Act
        await context.StartAsync();
        await context.WaitForCompletion(3000);

        // Assert
        context.Result!.Statistics.Should().NotBeNull();
        context.Result.Statistics!.Total.Should().Be(total);

        context.Result.Statistics.Succeeded.Should().Be(total - shouldFail);
        context.Result.Statistics.Succeeded.Should().Be(context.Succeded);

        context.Result.Statistics.Failed.Should().Be(shouldFail);
        context.Result.Statistics.Failed.Should().Be(context.Failed);

        context.StartConditionAttempts.Should().Be(1, "the operation should not run multiple startup attempts");
    }

    [Fact]
    public async Task Operation_WhenCancelled_ShouldCancelSomeTasks()
    {
        // Arrange
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = 5;
            t.DelayPerTask = TimeSpan.FromMilliseconds(1000);
        });

        // Act
        await context.StartAsync();
        await Task.Delay(2000);
        await context.CancelAsync();
        await context.WaitForCompletion(5000);

        // Assert
        context.Result!.Statistics.Should().NotBeNull();

        // total should be accurate
        context.Result.Statistics!.Total.Should().Be(5);

        // provided that we only allow 1 parallel task execution

        // there should be 1 succeded task 1 running (eventually succeded) = 2
        context.Result.Statistics.Succeeded.Should().BeGreaterThanOrEqualTo(0);
        // remaining tasks should be cancelled
        context.Result.Statistics.Cancelled.Should().BeGreaterThanOrEqualTo(1);
        // no failures were configured
        context.Result.Statistics.Failed.Should().Be(0);
    }

    [Fact]
    public async Task Operation_WhenErrorOnStart_ShouldFail()
    {
        // Arrange
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = 5;
            t.DelayPerTask = TimeSpan.FromMilliseconds(1000);
            t.ShoulFailOnStart = true;
        });

        // Act
        await context.StartAsync();
        await Task.Delay(100);
        await context.WaitForCompletion(5000);

        // Assert
        context.Result.Should().NotBeNull();

        context.Result!.Status.Should().Be(OperationStatus.Failed);
        context.Result!.Failure!.Type.Should().Be(OperationFailureType.ErrorCheckingCondition);
        context.StartConditionAttempts.Should().Be(1, "the operation should not run multiple startup attempts");
    }

    [Fact]
    public async Task Operation_WhenStartuPostponed_StartupCalledMultipleTimes()
    {
        // Arrange
        await using var context = new OperationContext<TestOperation.OperationData>();
        context.ConfigureOperation(t =>
        {
            t.TotalTasks = 5;
            t.SimulateStartupAttempts = 5;
        });

        // Act
        await context.StartAsync();
        await Task.Delay(100);
        await context.WaitForCompletion(5000);

        // Assert
        context.Result.Should().NotBeNull();

        context.Result!.Status.Should().Be(OperationStatus.Succeeded);
        context.Result!.Failure.Should().BeNull();
        context.StartConditionAttempts.Should().Be(5, "the operation must take 5 attempts to run startup code");
    }
}
