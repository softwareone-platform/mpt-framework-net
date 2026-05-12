using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mpt.Framework.MessageHub;
using Mpt.Framework.Persistence.Internal;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Abstract <see cref="IUnitOfWork"/> base. The EFCore add-on supplies a concrete
/// subclass; the in-memory engine ships a default subclass for tests.
/// </summary>
public abstract class UnitOfWork(IServiceProvider serviceProvider, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _cache = [];

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => SaveChangesInternalAsync(null, null, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(Func<CancellationToken, Task> afterSaveActivity, CancellationToken cancellationToken)
        => SaveChangesInternalAsync(afterSaveActivity, null, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(Func<CancellationToken, Task> afterSaveActivity, Func<Exception, CancellationToken, Task> afterSaveActivityFailure, CancellationToken cancellationToken)
        => SaveChangesInternalAsync(afterSaveActivity, afterSaveActivityFailure, cancellationToken);

    /// <summary>
    /// Drives the four-phase save flow across every registered repository — initiated
    /// → before save → save → after save (publish events) — then runs the optional
    /// after-save activity. Override to wrap the phase block in a transaction.
    /// </summary>
    protected virtual async Task SaveChangesInternalAsync(
        Func<CancellationToken, Task>? afterSaveActivity,
        Func<Exception, CancellationToken, Task>? afterSaveActivityFailure,
        CancellationToken cancellationToken)
    {
        var timestamp = DateTime.UtcNow;
        var repositories = _cache.Values.OfType<IPlatformRepository>().ToList();

        foreach (var repository in repositories)
            await repository.OnSaveChangesInitiatedAsync(timestamp, cancellationToken);

        foreach (var repository in repositories)
            await repository.OnBeforeSaveChangesAsync(timestamp, cancellationToken);

        foreach (var repository in repositories)
            await repository.SaveChangesAsync(cancellationToken);

        var publisher = serviceProvider.GetService<IMessageHubPublisher>();
        foreach (var repository in repositories)
            await repository.OnAfterSaveChangesAsync(publisher, cancellationToken);

        if (afterSaveActivity == null)
            return;

        try
        {
            await afterSaveActivity(cancellationToken);
        }
        catch (Exception exc) when (afterSaveActivityFailure is not null)
        {
            logger.LogWarning(exc, "After-save activity failed. Executing failure handler.");
            await afterSaveActivityFailure(exc, cancellationToken);
        }
        catch (Exception exc)
        {
            logger.LogError(exc, "After-save activity failed with unhandled exception.");
            throw;
        }
    }

    /// <inheritdoc />
    public IRepository<TEntity> GetRepository<TEntity>()
    {
        if (!_cache.TryGetValue(typeof(TEntity), out var repository))
        {
            repository = serviceProvider.GetRequiredService<IRepository<TEntity>>();
            _cache.Add(typeof(TEntity), repository);
        }

        return (IRepository<TEntity>)repository;
    }

    /// <inheritdoc />
    public void ResetChanges()
    {
        foreach (var repository in _cache.Values.OfType<IPlatformRepository>())
        {
            repository.ResetChanges();
            repository.ResetStorage();
        }
    }
}

/// <summary>
/// Default in-memory <see cref="UnitOfWork"/>. Used by the engine package's
/// <c>AddPersistence</c> registration; the EF Core add-on substitutes a transactional
/// implementation.
/// </summary>
public sealed class InMemoryUnitOfWork(IServiceProvider serviceProvider, ILogger<UnitOfWork> logger)
    : UnitOfWork(serviceProvider, logger);
