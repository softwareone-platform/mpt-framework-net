using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests;

public class EventMessageTests
{
    [Fact]
    public void Validate_FullyPopulatedMessage_DoesNotThrow()
    {
        var message = MakeValid();

        var act = () => message.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_EmptyObjectsCollection_ThrowsAboutSubject()
    {
        var message = MakeValid();
        message.Objects.Clear();

        var act = () => message.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("At least one subject must be specified");
    }

    [Fact]
    public void Validate_NullRouting_ThrowsAboutRouting()
    {
        var message = new EventMessage
        {
            Routing = null!,
            Objects = MakeValid().Objects,
            Info = MakeValid().Info,
        };

        var act = () => message.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Routing must be defined");
    }

    [Theory]
    [InlineData(StreamTypes.None)]
    [InlineData(StreamTypes.Events | StreamTypes.Sync)]
    [InlineData(StreamTypes.Events | StreamTypes.System)]
    [InlineData(StreamTypes.Sync | StreamTypes.System)]
    [InlineData(StreamTypes.Events | StreamTypes.Sync | StreamTypes.System)]
    public void Validate_StreamFlagsNoneOrCombined_ThrowsAboutStream(StreamTypes stream)
    {
        var message = MakeValid();
        message.Routing.Stream = stream;

        var act = () => message.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Routing stream cannot be combined or be None");
    }

    [Theory]
    [InlineData(StreamTypes.Events)]
    [InlineData(StreamTypes.Sync)]
    [InlineData(StreamTypes.System)]
    public void Validate_SingleStreamFlag_DoesNotThrow(StreamTypes stream)
    {
        var message = MakeValid();
        message.Routing.Stream = stream;

        var act = () => message.Validate();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrWhitespaceSummary_ThrowsAboutSummary(string? summary)
    {
        var message = MakeValid();
        message.Info = new EventMessageInfo { Summary = summary! };

        var act = () => message.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Summary is required");
    }

    [Fact]
    public void Validate_NullInfo_ThrowsAboutSummary()
    {
        var message = MakeValid();
        message.Info = null!;

        var act = () => message.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Summary is required");
    }

    private static EventMessage MakeValid() => new()
    {
        Id = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        Info = new EventMessageInfo { Summary = "ok" },
        Routing = new EventMessageRouting
        {
            Stream = StreamTypes.Events,
            SourceModule = "billing",
            Entity = "Invoice",
            Event = "Created",
        },
        Objects =
        [
            new EventMessageObject
            {
                Id = "1",
                Key = "invoice",
                Category = EventMessageObjectCategory.CurrentEntity,
                Data = new { id = 1 },
            }
        ],
    };
}
