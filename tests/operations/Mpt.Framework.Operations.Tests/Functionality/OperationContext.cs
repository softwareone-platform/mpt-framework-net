using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Framework.Operations;
using Mpt.Framework.Operations.Configuration;

namespace Mpt.Framework.Operations.Tests.Functionality;

public class OperationContext<TOperation> : IAsyncDisposable
    where TOperation : IOperationContract, new()
{
    private volatile int _succeded;
    private volatile int _failed;

    private IOperationDispatcher? _dispatcher;
    private IBusControl? _busControl;
    private Guid? _opearationId;

    public void ConfigureOperation(Action<OperationConfig> configure)
    {
        Config = new OperationConfig();
        configure(Config);
    }

    public async Task StartAsync()
    {
        // Arrange
        var provider = MakeOperationsProvider();
        var scope = provider.CreateScope();
        _dispatcher = scope.ServiceProvider.GetRequiredService<IOperationDispatcher>();
        _busControl = (provider.GetRequiredService<IOperationsBus>() as IBusControl)!;

        // Act
        await _busControl.StartAsync();
        _opearationId = await _dispatcher.DispatchAsync(new TOperation(), CancellationToken.None);
    }

    public Task WaitForCompletion(int timeoutMs)
        => WaitForCondition(t => t.Result != null, timeoutMs);

    public async Task CancelAsync()
    {
        if (_dispatcher == null || !_opearationId.HasValue)
        {
            return;
        }

        await _dispatcher.CancelAsync<TOperation>(_opearationId.Value, CancellationToken.None);
    }

    public void ReportTaskComplete(bool isSuccess)
    {
        if (isSuccess)
        {
            _succeded++;
        }
        else
        {
            _failed++;
        }
    }

    public void ReportOperationComplete(OperationResult result)
    {
        Result = result;
    }

    public int StartConditionAttempts { get; set; } = 0;

    public int Succeded => _succeded;

    public int Failed => _failed;

    public OperationResult? Result { get; private set; }

    public OperationConfig Config { get; private set; } = null!;

    private async Task WaitForCondition(Func<OperationContext<TOperation>, bool> condition, int? timeout = null)
    {
        var startTime = DateTime.UtcNow;

        while (true)
        {
            if (timeout.HasValue && (DateTime.UtcNow - startTime).TotalMilliseconds > timeout)
            {
                throw new TimeoutException();
            }

            if (condition(this))
            {
                break;
            }

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

    public bool ShoulFailOnStart { get; set; }

    public int SimulateStartupAttempts { get; set; } = 1;

    public TimeSpan DelayPerTask { get; set; }

    public TimeSpan DelayBeforeProduceTasks { get; set; }
}
