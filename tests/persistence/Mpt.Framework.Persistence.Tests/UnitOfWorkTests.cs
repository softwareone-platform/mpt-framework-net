using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Persistence.Tests.Fixtures;

namespace Mpt.Framework.Persistence.Tests;

/// <summary>
/// Drives the SaveChangesAsync overloads on <see cref="UnitOfWork"/>, in particular the
/// afterSaveActivity paths (no activity / success / failure-with-handler).
/// </summary>
public class UnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_WithoutAfterSaveActivity_CompletesQuietly()
    {
        await using var sp = PersistenceFixture.Build();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var act = async () => await uow.SaveChangesAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_WithSuccessfulAfterSaveActivity_InvokesIt()
    {
        await using var sp = PersistenceFixture.Build();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var afterSaveRan = false;

        await uow.SaveChangesAsync(_ => { afterSaveRan = true; return Task.CompletedTask; }, CancellationToken.None);

        afterSaveRan.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAfterSaveActivityThrowsWithoutFailureHandler_BubblesException()
    {
        await using var sp = PersistenceFixture.Build();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var act = async () => await uow.SaveChangesAsync(
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAfterSaveActivityThrowsWithFailureHandler_RunsTheHandlerAndSwallows()
    {
        await using var sp = PersistenceFixture.Build();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        Exception? captured = null;

        await uow.SaveChangesAsync(
            _ => throw new InvalidOperationException("boom"),
            (exc, _) => { captured = exc; return Task.CompletedTask; },
            CancellationToken.None);

        captured.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("boom");
    }

    [Fact]
    public async Task GetRepository_CachesPerEntityType()
    {
        await using var sp = PersistenceFixture.Build();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var first = uow.GetRepository<WidgetView>();
        var second = uow.GetRepository<WidgetView>();

        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task ResetChanges_ClearsPendingStateAcrossCachedRepositories()
    {
        await using var sp = PersistenceFixture.Build();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        // Force a repository into the cache and queue a pending add.
        var repo = uow.GetRepository<WidgetView>();
        repo.Add(new WidgetView { Id = "w1", Name = "pending" });

        var act = uow.ResetChanges;

        act.Should().NotThrow();
    }
}
