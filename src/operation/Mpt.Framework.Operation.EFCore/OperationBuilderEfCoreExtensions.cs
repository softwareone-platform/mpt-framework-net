using Mpt.Framework.Operation.EFCore;

namespace Mpt.Framework.Operation;

public static class OperationBuilderEfCoreExtensions
{
    /// <summary>
    /// Configures the operation engine to persist sagas in SQL Server via Entity Framework Core.
    /// The dedicated <c>OperationDbContext</c> is registered for you; you still need to call
    /// <c>modelBuilder.AddOperationEntity()</c> in your primary <c>DbContext.OnModelCreating</c>
    /// and create the matching migration.
    /// </summary>
    public static OperationBuilder UseSqlServerPersistence(this OperationBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        builder.Persistence = new SqlServerPersistenceProvider(connectionString);
        return builder;
    }
}
