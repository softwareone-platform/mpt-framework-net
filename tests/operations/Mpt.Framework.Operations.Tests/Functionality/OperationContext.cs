using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Operations.Configuration;

namespace Mpt.Framework.Operations.Tests.Functionality;

/// <summary>
/// Spins up a self-contained in-memory operations engine in DI, dispatches a single operation of the
/// requested type, and exposes hooks so the <see cref="TestOperation"/> handler can report task /
/// operation completion back to the test code.
/// </summary>
public class OperationContext<TOperation> : IAsyncDisposable
    where TOperation : IOperationContract, new()
{
    private volatile int _succeeded;
    private volatile int _failed;

    private IOperationDispatcher? _dispatcher;
    private IBusControl? _busControl;
    private Guid? _operationId;

    public void ConfigureOperation(Action<OperationConfig> configure)
    {
        Config = new OperationConfig();
        configure(Config);
    }

    public async Task StartAsync()
    {
        var provider = MakeOperationsProvider();
        var scope = provider.CreateScope();
        _dispatcher = scope.ServiceProvider.GetRequiredService<IOperationDispatcher>();
        _busControl = (provider.GetRequiredService<IOperationsBus>() as IBusControl)!;

        await _busControl.StartAsync();
        _operationId = await _dispatcher.DispatchAsync(new TOperation(), CancellationToken.None);
    }

    public Task WaitForCompletion(int timeoutMs)
        => WaitForCondition(t => t.Result != null, timeoutMs);

    public async Task CancelAsync()
    {
        if (_dispatcher == null || !_operationId.HasValue)
            return;

        await _dispatcher.CancelAsync<TOperation>(_operationId.Value, CancellationToken.None);
    }

    public void ReportTaskComplete(bool isSuccess)
    {
        if (isSuccess)
            _succeeded++;
        else
            _failed++;
    }

    public void ReportOperationComplete(OperationResult result) => Result = result;

    public int StartConditionAttempts { get; set; }

    public int Succeeded => _succeeded;

    public int Failed => _failed;

    public OperationResult? Result { get; private set; }

    public OperationConfig Config { get; private set; } = null!;

    private async Task WaitForCondition(Func<OperationContext<TOperation>, bool> condition, int? timeout = null)
    {
        var startTime = DateTime.UtcNow;

        while (true)
        {
            if (timeout.HasValue && (DateTime.UtcNow - startTime).TotalMilliseconds > timeout)
                throw new TimeoutException();

            if (condition(this))
                break;

            await Task.Delay(50);
        }
    }

    private IServiceProvider MakeOperationsProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOperations("test", ops =>
        {
            ops.Settings.Mode = OperationsMode.ConsumeAndDispatch;
            ops.Settings.Transport = OperationsTransport.InMemory;
            ops.Register<TestOperation>("test-operation", t => t.Tasks.Concurrency = 1);
        });

        services.AddSingleton(this);

        return services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        if (_busControl != null)
        {
            await _busControl.StopAsync();
            _busControl = null;
        }

        GC.SuppressFinalize(this);
    }
}

public class OperationConfig
{
    public int TotalTasks { get; set; }

    public int ShouldFailTask { get; set; }

    public bool ShouldFailOnStart { get; set; }

    public bool ShouldThrowInGetTasks { get; set; }

    public bool AllMustSucceed { get; set; }

    public int SimulateStartupAttempts { get; set; } = 1;

    public TimeSpan DelayPerTask { get; set; }

    public TimeSpan DelayBeforeProduceTasks { get; set; }
}
