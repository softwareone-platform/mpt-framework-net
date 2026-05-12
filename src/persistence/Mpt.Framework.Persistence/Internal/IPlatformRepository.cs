namespace Mpt.Framework.Persistence.Internal;

internal interface IPlatformRepository : IRepository
{
    void ResetStorage();

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task OnSaveChangesInitiatedAsync(DateTime timestamp, CancellationToken cancellationToken);

    Task OnBeforeSaveChangesAsync(DateTime timestamp, CancellationToken cancellationToken);

    Task OnAfterSaveChangesAsync(CancellationToken cancellationToken);
}
