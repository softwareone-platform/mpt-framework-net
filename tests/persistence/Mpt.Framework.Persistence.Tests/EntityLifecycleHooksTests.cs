using FluentAssertions;
using Mpt.Framework.Persistence.Internal;

namespace Mpt.Framework.Persistence.Tests;

public class EntityLifecycleHooksTests
{
    [Fact]
    public async Task OnCreatingAsync_ReturnsCompletedTask()
    {
        var hooks = new EntityLifecycleHooks<FakeEntity>();
        var context = new EntityActionContext<FakeEntity>(new FakeEntity(), DateTime.UtcNow);

        var task = hooks.OnCreatingAsync(context, CancellationToken.None);

        task.IsCompletedSuccessfully.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task OnUpdatingAsync_ReturnsCompletedTask()
    {
        var hooks = new EntityLifecycleHooks<FakeEntity>();
        var entity = new FakeEntity();
        var context = new EntityUpdatingContext<FakeEntity>(entity, entity, DateTime.UtcNow);

        var task = hooks.OnUpdatingAsync(context, CancellationToken.None);

        task.IsCompletedSuccessfully.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task OnDeletingAsync_ReturnsCompletedTask()
    {
        var hooks = new EntityLifecycleHooks<FakeEntity>();
        var context = new EntityActionContext<FakeEntity>(new FakeEntity(), DateTime.UtcNow);

        var task = hooks.OnDeletingAsync(context, CancellationToken.None);

        task.IsCompletedSuccessfully.Should().BeTrue();
        await task;
    }

    private sealed class FakeEntity : IPlatformEntity
    {
        public string Id { get; set; } = "fake-id";
        public int Revision { get; set; }
    }
}
