using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Persistence.Tests.Fixtures;

namespace Mpt.Framework.Persistence.Tests;

public class LifecycleHookTests
{
    [Fact]
    public async Task OnCreatingAsync_fires_with_the_added_entity()
    {
        var hooks = new RecordingHooks();
        using var services = PersistenceFixture.Build(configureServices: s => s.AddSingleton<IEntityLifecycleHooks<WidgetView>>(hooks));
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        repo.Add(new WidgetView { Id = "w1", Name = "new", Count = 0 });
        await uow.SaveChangesAsync(CancellationToken.None);

        hooks.CreateCalls.Should().ContainSingle(c => c.Entity.Id == "w1");
        hooks.UpdateCalls.Should().BeEmpty();
        hooks.DeleteCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task OnUpdatingAsync_fires_with_both_original_and_current_state()
    {
        var hooks = new RecordingHooks();
        using var services = PersistenceFixture.Build(configureServices: s => s.AddSingleton<IEntityLifecycleHooks<WidgetView>>(hooks));
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.Add(new WidgetDbEntity { Id = "w1", Name = "before", Count = 1 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var view = await repo.GetForUpdateOrThrowAsync("w1", CancellationToken.None);
        view.Name = "after";

        await uow.SaveChangesAsync(CancellationToken.None);

        hooks.UpdateCalls.Should().ContainSingle();
        var (original, current) = hooks.UpdateCalls[0];
        original.Name.Should().Be("before");
        current.Name.Should().Be("after");
    }

    [Fact]
    public async Task OnDeletingAsync_fires_with_the_target_entity()
    {
        var hooks = new RecordingHooks();
        using var services = PersistenceFixture.Build(configureServices: s => s.AddSingleton<IEntityLifecycleHooks<WidgetView>>(hooks));
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.Add(new WidgetDbEntity { Id = "w1", Name = "doomed", Count = 0 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var view = await repo.GetForUpdateOrThrowAsync("w1", CancellationToken.None);
        repo.Delete(view);
        await uow.SaveChangesAsync(CancellationToken.None);

        hooks.DeleteCalls.Should().ContainSingle(c => c.Entity.Id == "w1");
    }

    private sealed class RecordingHooks : IEntityLifecycleHooks<WidgetView>
    {
        public List<IEntityActionContext<WidgetView>> CreateCalls { get; } = [];
        public List<(WidgetView Original, WidgetView Current)> UpdateCalls { get; } = [];
        public List<IEntityActionContext<WidgetView>> DeleteCalls { get; } = [];

        public Task OnCreatingAsync(IEntityActionContext<WidgetView> context, CancellationToken cancellationToken)
        {
            CreateCalls.Add(context);
            return Task.CompletedTask;
        }

        public Task OnUpdatingAsync(IEntityUpdatingContext<WidgetView> context, CancellationToken cancellationToken)
        {
            // Snapshot — the engine reuses the entity instance later, so capture values now.
            UpdateCalls.Add((
                new WidgetView { Id = context.Original.Id, Name = context.Original.Name, Count = context.Original.Count },
                new WidgetView { Id = context.Entity.Id, Name = context.Entity.Name, Count = context.Entity.Count }));
            return Task.CompletedTask;
        }

        public Task OnDeletingAsync(IEntityActionContext<WidgetView> context, CancellationToken cancellationToken)
        {
            DeleteCalls.Add(context);
            return Task.CompletedTask;
        }
    }
}
