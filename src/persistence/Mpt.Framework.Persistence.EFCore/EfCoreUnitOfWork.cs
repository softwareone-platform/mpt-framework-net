using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Mpt.Framework.Persistence;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.Persistence.EFCore;

/// <summary>
/// EF Core-aware <see cref="UnitOfWork"/>. Wraps the full save-flow in a single
/// <see cref="IDbContextTransaction"/> when the underlying provider supports it, so
/// that domain events emitted in the after-save phase only fire after the SQL
/// transaction successfully commits.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Transaction wrapper around UnitOfWork; the supports-transactions branch requires a real relational provider so it cannot be exercised under the InMemory test fixture.")]
public class EfCoreUnitOfWork(
    IServiceProvider serviceProvider,
    ILogger<UnitOfWork> logger,
    DbContext dbContext)
    : UnitOfWork(serviceProvider, logger)
{
    /// <inheritdoc />
    protected override async Task SaveChangesInternalAsync(
        Func<CancellationToken, Task>? afterSaveActivity,
        Func<Exception, CancellationToken, Task>? afterSaveActivityFailure,
        CancellationToken cancellationToken)
    {
        // Providers that don't support transactions (the InMemory provider, primarily)
        // expose CurrentTransaction as null and reject BeginTransactionAsync — skip the
        // wrapper in that case.
        var providerName = dbContext.Database.ProviderName ?? string.Empty;
        var supportsTransactions = !providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase);

        if (supportsTransactions)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await base.SaveChangesInternalAsync(afterSaveActivity, afterSaveActivityFailure, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await base.SaveChangesInternalAsync(afterSaveActivity, afterSaveActivityFailure, cancellationToken);
        }
    }
}
