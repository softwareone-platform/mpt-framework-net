using MassTransit;
using System.Text.Json.Nodes;

namespace Mpt.Framework.Operations.Models;

public class OperationSaga : SagaStateMachineInstance, ISagaVersion
{
    public OperationSaga()
    {
        Timestamps = new OperationSagaTimestamps
        {
            Created = DateTimeOffset.UtcNow
        };
        StartCondition = new OperationSagaStartCondition { Attempt = 1 };
    }

    public OperationSaga(string type) : this()
    {
        Type = type;
    }

    public Guid CorrelationId { get; set; }

    public string Type { get; set; } = null!;

    public string? Status { get; set; } = null!;

    public OperationSagaStatistics Statistics { get; set; } = new();

    public byte[]? TaskStates { get; set; }

    public OperationSagaTimestamps Timestamps { get; set; } = new();

    public JsonObject? Data { get; set; }

    public OperationSagaStartCondition StartCondition { get; set; } = new();

    public OperationFailure? Failure { get; set; }

    public int Version { get; set; }
}

public class OperationSagaStatistics
{
    public int Total { get; set; }

    public int Succeded { get; set; }

    public int Failed { get; set; }

    public int Pending { get; set; }

    public int Cancelled { get; set; }
}

public class OperationSagaTimestamps
{
    public DateTimeOffset? Created { get; set; }

    public DateTimeOffset? Started { get; set; }

    public DateTimeOffset? Finished { get; set; }
}

public class OperationSagaStartCondition
{
    public int Attempt { get; set; }

    public bool IsSatisfied { get; set; }
}
