namespace Mpt.Framework.MessageHub.Tests.Events;

/// <summary>Shared <see cref="IPlatformEntity"/> used by Events tests.</summary>
public class TestEntity : IPlatformEntity
{
    public string Id { get; set; } = "test-id";
    public int Revision { get; set; }
    public string? Status { get; set; }
}
