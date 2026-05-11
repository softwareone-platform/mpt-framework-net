using Mpt.Framework.Operations.EntityFrameworkCore;

namespace Mpt.Framework.Operations;

public static class OperationsBuilderEntityFrameworkExtensions
{
    /// <summary>
    /// Configures the operations engine to persist sagas in SQL Server via Entity Framework Core.
    /// The dedicated <c>OperationsDbContext</c> is registered for you; you still need to call
    /// <c>modelBuilder.AddOperationsEntity()</c> in your primary <c>DbContext.OnModelCreating</c>
    /// and create the matching migration.
    /// </summary>
    public static OperationsBuilder UseSqlServerPersistence(this OperationsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        builder.Persistence = new SqlServerPersistenceProvider(connectionString);
        return builder;
    }
}
