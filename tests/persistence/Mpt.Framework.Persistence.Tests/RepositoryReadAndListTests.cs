using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Persistence.Tests.Fixtures;
using Mpt.Rql;

namespace Mpt.Framework.Persistence.Tests;

public class RepositoryReadAndListTests
{
    [Fact]
    public async Task GetAsync_ReturnsView_WhenEntityExists()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.Add(new WidgetDbEntity { Id = "w1", Name = "alpha", Count = 7 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var view = await repo.GetAsync("w1", CancellationToken.None);

        view.Should().NotBeNull();
        view!.Name.Should().Be("alpha");
        view.Count.Should().Be(7);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenMissing()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var view = await repo.GetAsync("missing", CancellationToken.None);

        view.Should().BeNull();
    }

    [Fact]
    public async Task GetReferenceAsync_LoadsReference()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.Add(new WidgetDbEntity { Id = "w1", Name = "alpha", Count = 7 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var reference = await repo.GetReferenceAsync("w1", CancellationToken.None);

        reference.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReferenceAsObjectAsync_BoxesTheResult()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.Add(new WidgetDbEntity { Id = "w1", Name = "alpha", Count = 7 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = (IRepository)uow.GetRepository<WidgetView>();
        var reference = await repo.GetReferenceAsObjectAsync("w1", CancellationToken.None);

        reference.Should().NotBeNull();
        reference.Should().BeAssignableTo<WidgetView>();
    }

    [Fact]
    public async Task GetShapedAsync_ProjectsToCustomShape()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.Add(new WidgetDbEntity { Id = "w1", Name = "alpha", Count = 11 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var shaped = await repo.GetShapedAsync("w1", w => new { w.Id, Doubled = w.Count * 2 }, CancellationToken.None);

        shaped.Should().NotBeNull();
        shaped!.Id.Should().Be("w1");
        shaped.Doubled.Should().Be(22);
    }

    [Fact]
    public async Task ListAsync_AppliesFilter_ReturnsMatchingRows()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.AddRange(
            new WidgetDbEntity { Id = "w1", Name = "alpha", Count = 1 },
            new WidgetDbEntity { Id = "w2", Name = "alpha", Count = 2 },
            new WidgetDbEntity { Id = "w3", Name = "beta", Count = 3 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var rows = await repo.ListAsync(w => w.Name == "alpha", CancellationToken.None);

        rows.Should().HaveCount(2);
        rows.Should().OnlyContain(w => w.Name == "alpha");
    }

    [Fact]
    public async Task ListForUpdateAsync_LoadsAndMarksForUpdate_OnSaveBumpsRevisionsOfMutated()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.AddRange(
            new WidgetDbEntity { Id = "w1", Name = "a", Count = 1 },
            new WidgetDbEntity { Id = "w2", Name = "a", Count = 2 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var rows = await repo.ListForUpdateAsync(w => w.Name == "a", CancellationToken.None);

        rows.Should().HaveCount(2);
        rows[0].Count = 100;

        await uow.SaveChangesAsync(CancellationToken.None);

        var w1 = await db.Widgets.AsNoTracking().FirstAsync(w => w.Id == rows[0].Id);
        var w2 = await db.Widgets.AsNoTracking().FirstAsync(w => w.Id == rows[1].Id);
        w1.Revision.Should().Be(1);
        w2.Revision.Should().Be(0);
    }

    [Fact]
    public async Task CountAsync_WithFilter_ReturnsMatchCount()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.AddRange(
            new WidgetDbEntity { Id = "w1", Name = "alpha", Count = 1 },
            new WidgetDbEntity { Id = "w2", Name = "alpha", Count = 2 },
            new WidgetDbEntity { Id = "w3", Name = "beta", Count = 3 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var count = await repo.CountAsync(w => w.Name == "alpha", CancellationToken.None);

        count.Should().Be(2);
    }

    [Fact]
    public async Task ResetChanges_DiscardsPendingAddBeforeSave()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();

        repo.Add(new WidgetView { Id = "w1", Name = "discarded", Count = 1 });
        repo.ResetChanges();
        await uow.SaveChangesAsync(CancellationToken.None);

        var rows = await db.Widgets.AsNoTracking().ToListAsync();
        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ListForUpdate_thenSave_keepsRevisionWhenNothingChanged()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.AddRange(
            new WidgetDbEntity { Id = "w1", Name = "x", Count = 1 },
            new WidgetDbEntity { Id = "w2", Name = "x", Count = 2 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var rows = await repo.ListForUpdateAsync(w => w.Name == "x", CancellationToken.None);

        rows.Should().HaveCount(2);
        // No mutations.

        await uow.SaveChangesAsync(CancellationToken.None);

        var fresh = await db.Widgets.AsNoTracking().OrderBy(w => w.Id).ToListAsync();
        fresh.Should().AllSatisfy(w => w.Revision.Should().Be(0));
    }

    [Fact]
    public async Task AddThenDelete_InSameUnitOfWork_LeavesRowsEmpty()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();

        var w = new WidgetView { Id = "w1", Name = "a", Count = 1 };
        repo.Add(w);
        repo.Delete(w);

        await uow.SaveChangesAsync(CancellationToken.None);

        var rows = await db.Widgets.AsNoTracking().ToListAsync();
        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task CountAsync_WithRqlRequest_ReturnsMatchCount()
    {
        // Exercises the IRepository<T>.CountAsync(RqlRequest, ct) default-interface
        // overload (forwards to CountAsync(filter, configure, ct) with the request in
        // the options bag).
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.AddRange(
            new WidgetDbEntity { Id = "w1", Name = "alpha", Count = 1 },
            new WidgetDbEntity { Id = "w2", Name = "beta", Count = 2 },
            new WidgetDbEntity { Id = "w3", Name = "alpha", Count = 3 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var count = await repo.CountAsync(new RqlRequest(), CancellationToken.None);

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetOrThrowAsync_WithConfigure_ReturnsEntityWhenFound()
    {
        // Exercises the IRepository<T>.GetOrThrowAsync(id, configure, ct) default-interface
        // overload (the one that takes a configure delegate and forwards to GetAsync).
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<WidgetDbContext>();
        db.Widgets.Add(new WidgetDbEntity { Id = "w1", Name = "alpha", Count = 7 });
        await db.SaveChangesAsync();

        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();
        var view = await repo.GetOrThrowAsync("w1", static _ => { }, CancellationToken.None);

        view.Should().NotBeNull();
        view.Name.Should().Be("alpha");
    }

    [Fact]
    public async Task GetOrThrowAsync_WithConfigure_ThrowsWhenMissing()
    {
        using var services = PersistenceFixture.Build();
        using var scope = services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repo = uow.GetRepository<WidgetView>();

        var act = async () => await repo.GetOrThrowAsync("missing", static _ => { }, CancellationToken.None);
        await act.Should().ThrowAsync<PersistenceEntityNotFoundException>();
    }
}
