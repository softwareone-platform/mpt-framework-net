using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Operations.Configuration;
using Mpt.Framework.Operations.Utility;

namespace Mpt.Framework.Operations;

public class OperationsBuilder
{
    private readonly string _module;
    private readonly Dictionary<Type, OperationDescriptor> _cache = [];

    internal OperationsBuilder(IServiceCollection services, string moduleCode, OperationSettings settings)
    {
        if (string.IsNullOrWhiteSpace(moduleCode))
            throw new ArgumentException("Module code cannot be null or empty", nameof(moduleCode));

        _module = moduleCode.ToLowerInvariant();
        Services = services;
        Settings = settings;
    }

    public IServiceCollection Services { get; }

    public OperationSettings Settings { get; }

    /// <summary>
    /// Persistence provider for operation sagas. Defaults to in-memory; use a companion package
    /// (e.g. <c>Mpt.Framework.Operations.EntityFrameworkCore</c>) to install a durable provider.
    /// </summary>
    public IOperationsPersistenceProvider Persistence { get; set; } = new InMemoryOperationsPersistenceProvider();

    public OperationsBuilder Register<TOperation>(string name, Action<ProcessingOptions>? configure = null)
        where TOperation : IOperation
    {
        OperationValidator.ValidateName(name, "Operation Name");

        var operationInterface = typeof(TOperation).GetInterfaces().Where(t => t.IsGenericType)
                .Select(s => new { GenericDefinition = s.GetGenericTypeDefinition(), Type = s })
                .FirstOrDefault(t => t.GenericDefinition == typeof(IOperation<,>))
                ?? throw new InvalidOperationException($"Operation {typeof(TOperation).Name} must implement IOperation<,> interface");

        var implementationType = typeof(TOperation);

        var desc = new OperationDescriptor
        {
            Name = name,
            ModuleCode = _module,
            GlobalPrefix = Settings.GlobalPrefix,
            SagaType = OperationSagaTypeBuilder.MakeSagaType(implementationType, name),
            ImplementationType = implementationType,
            OperationType = operationInterface.Type.GenericTypeArguments[0],
            TaskType = operationInterface.Type.GenericTypeArguments[1],
        };

        if (_cache.ContainsKey(desc.OperationType))
            throw new InvalidOperationException($"Configuration for operation {desc.ImplementationType.Name} is already registered");

        configure?.Invoke(desc.Processing);

        if (Settings.Mode == OperationsMode.ConsumeAndDispatch)
        {
            Services.AddScoped(operationInterface.Type, implementationType);
            Services.AddScoped(implementationType);
        }

        _cache.Add(desc.OperationType, desc);

        return this;
    }

    internal IReadOnlyDictionary<Type, OperationDescriptor> Descriptors => _cache;
}
