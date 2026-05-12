using Mpt.Framework.MessageHub;

namespace Mpt.Framework.Persistence.Internal;

/// <summary>
/// Internal contract <see cref="UnitOfWork"/> uses to coordinate the multi-phase save
/// flow across every registered repository. Exposed as <see langword="internal"/> in
/// the engine assembly + <c>InternalsVisibleTo</c> for the EFCore add-on and tests.
/// </summary>
internal interface IPlatformRepository : IRepository
{
    void ResetStorage();

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task OnSaveChangesInitiatedAsync(DateTime timestamp, CancellationToken cancellationToken);

    Task OnBeforeSaveChangesAsync(DateTime timestamp, CancellationToken cancellationToken);

    Task OnAfterSaveChangesAsync(IMessageHubPublisher? publisher, CancellationToken cancellationToken);
}
