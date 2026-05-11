using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Mpt.Framework.Operation.Configuration;

/// <summary>
/// Pluggable persistence provider for the operation engine. The main package ships an in-memory
/// implementation; a companion package may register an alternative (for example,
/// <c>Mpt.Framework.Operation.EFCore</c> registers a SQL Server / EF Core provider).
/// </summary>
public interface IOperationPersistenceProvider
{
    /// <summary>
    /// Called once when <c>AddOperation</c> runs, after operation descriptors are known and before
    /// MassTransit is wired up. Use this to register the saga store (DbContext, etc.) in DI.
    /// </summary>
    void RegisterServices(IServiceCollection services, OperationSettings settings, IReadOnlyCollection<OperationSagaRegistration> registrations);

    /// <summary>
    /// Called once per registered operation to configure the MassTransit saga repository for the
    /// given state machine.
    /// </summary>
    void ConfigureSagaRepository<TSaga>(ISagaRegistrationConfigurator<TSaga> configurator)
        where TSaga : class, ISaga;

    /// <summary>
    /// Hook for the persistence provider to add bus observers or extra wiring while the bus is being
    /// configured. The default in-memory provider does nothing here.
    /// </summary>
    void ConfigureBus<T>(IBusRegistrationContext context, IBusFactoryConfigurator<T> configurator, OperationSettings settings)
        where T : IReceiveEndpointConfigurator;
}

/// <summary>
/// Per-operation saga registration metadata that the persistence provider may need (e.g. to
/// populate an EF Core discriminator column).
/// </summary>
/// <param name="SagaType">Dynamically generated saga CLR type for this operation.</param>
/// <param name="OperationName">Stable operation name as provided to <c>OperationBuilder.Register</c>.</param>
public sealed record OperationSagaRegistration(Type SagaType, string OperationName);
