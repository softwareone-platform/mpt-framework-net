using Microsoft.EntityFrameworkCore;

namespace Mpt.Framework.Persistence.Tests.Fixtures;

public sealed class WidgetDbContext(DbContextOptions<WidgetDbContext> options) : DbContext(options)
{
    public DbSet<WidgetDbEntity> Widgets => Set<WidgetDbEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WidgetDbEntity>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Id).IsRequired();
            e.Property(w => w.Revision);
            e.Property(w => w.Name);
            e.Property(w => w.Count);
        });
    }
}
