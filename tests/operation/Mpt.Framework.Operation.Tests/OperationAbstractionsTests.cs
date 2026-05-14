using FluentAssertions;

namespace Mpt.Framework.Operation.Tests;

public class OperationAbstractionsTests
{
    [Fact]
    public void Operation_AllMustSucceed_DefaultsToTrue()
    {
        var op = new DummyOperation();

        op.AllMustSucceed.Should().BeTrue();
    }

    [Fact]
    public void Operation_AllMustSucceed_CanBeOverridden()
    {
        var op = new BestEffortOperation();

        op.AllMustSucceed.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 1, true)]
    [InlineData(5, 5, true)]
    [InlineData(5, 4, false)]
    [InlineData(5, 0, false)]
    public void OperationStatistics_AllSucceeded_ComparesTotalToSucceeded(int total, int succeeded, bool expected)
    {
        var stats = new OperationStatistics { Total = total, Succeeded = succeeded };

        stats.AllSucceeded().Should().Be(expected);
    }

    public class DummyOperationContract : IOperationContract
    {
        public string Id { get; set; } = string.Empty;
    }

    public class DummyTaskContract;

    public class DummyOperation : Operation<DummyOperationContract, DummyTaskContract>
    {
        public override async IAsyncEnumerable<DummyTaskContract> GetTasksAsync(
            IOperationPreparingContext<DummyOperationContract> context,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }

        public override Task<TaskResult> ProcessTaskAsync(IProcessTaskContext<DummyTaskContract> context, CancellationToken cancellationToken)
            => Task.FromResult(TaskResult.Success);
    }

    public class BestEffortOperation : DummyOperation
    {
        public override bool AllMustSucceed => false;
    }
}
