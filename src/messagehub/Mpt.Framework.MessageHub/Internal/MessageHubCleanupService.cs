using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.MessageHub.Internal;

/// <summary>
/// Hosted service that runs once on startup to delete stale subscriptions that no longer
/// correspond to a declared input stream. Only active when the transport is Service Bus
/// and <see cref="MessageHubSettings.CleanupMode"/> is not <see cref="MessageHubCleanupMode.None"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Hosted service that calls into the [ExcludeFromCodeCoverage] InputStreamBuilder — only useful against a real Azure Service Bus admin endpoint.")]
internal class MessageHubCleanupService(InputStreamBuilder streamBuilder) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => streamBuilder.CleanupAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
