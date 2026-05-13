using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Operation.EFCore;

[ExcludeFromCodeCoverage(Justification = "EF Core DbContext for the saga store — exercised end-to-end against a real database, not via unit tests.")]
internal class OperationDbContext(DbContextOptions<OperationDbContext> options, OperationSagaTypes sagaTypes) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OperationSagaEntityConfiguration(sagaTypes.Types));
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        IncrementSagaVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        IncrementSagaVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void IncrementSagaVersions()
    {
        var entries = ChangeTracker.Entries<Models.OperationSaga>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.Version++;
        }
    }
}
