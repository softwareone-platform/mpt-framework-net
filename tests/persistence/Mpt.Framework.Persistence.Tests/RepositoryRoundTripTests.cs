using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Persistence.Tests.Fixtures;

namespace Mpt.Framework.Persistence.Tests;

public class RepositoryRoundTripTests
{
    [Fact]
    public async Task Add_then_SaveChanges_persists_the_row()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();

        repo.Add(new WidgetView { Id = "w1", Name = "first", Count = 1 });
        await uow.SaveChangesAsync(CancellationToken.None);

        var row = await db.Widgets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == "w1");
        row.Should().NotBeNull();
        row!.Name.Should().Be("first");
        row.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetForUpdate_then_mutate_then_SaveChanges_updates_the_row_and_bumps_revision()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.Add(new WidgetDbEntity { Id = "w1", Name = "before", Count = 1 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();

        var view = await repo.GetForUpdateOrThrowAsync("w1", CancellationToken.None);
        view.Name = "after";
        view.Count = 7;

        await uow.SaveChangesAsync(CancellationToken.None);

        var row = await db.Widgets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == "w1");
        row!.Name.Should().Be("after");
        row.Count.Should().Be(7);
        row.Revision.Should().Be(1);
    }

    [Fact]
    public async Task GetForUpdate_then_no_changes_then_SaveChanges_leaves_revision_alone()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.Add(new WidgetDbEntity { Id = "w1", Name = "same", Count = 5 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();

        var view = await repo.GetForUpdateOrThrowAsync("w1", CancellationToken.None);
        // No mutation.

        await uow.SaveChangesAsync(CancellationToken.None);

        var row = await db.Widgets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == "w1");
        row!.Revision.Should().Be(0);
    }

    [Fact]
    public async Task Delete_then_SaveChanges_removes_the_row()
    {
        using var services = PersistenceFixture.Build();
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

        var rows = await db.Widgets.AsNoTracking().ToListAsync();
        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrThrow_when_missing_throws_PersistenceEntityNotFoundException()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();

        var act = async () => await repo.GetOrThrowAsync("missing", CancellationToken.None);
        await act.Should().ThrowAsync<PersistenceEntityNotFoundException>()
            .Where(ex => ex.Id.Equals("missing"));
    }
}
