using FluentAssertions;
using Mpt.Framework.Persistence.Internal;
using Mpt.Rql.Abstractions.Configuration;
using NSubstitute;

namespace Mpt.Framework.Persistence.Tests;

public class GetEntityOptionsTests
{
    [Fact]
    public void Configure_StoresActionThatApplyConfigurationInvokes()
    {
        var options = new GetEntityOptions();
        var calls = 0;

        options.Configure(_ => calls++);
        options.ApplyConfiguration(Substitute.For<IRqlSettings>());

        calls.Should().Be(1);
    }

    [Fact]
    public void ApplyConfiguration_WhenNoConfigureWasCalled_IsANoOp()
    {
        var options = new GetEntityOptions();

        var act = () => options.ApplyConfiguration(Substitute.For<IRqlSettings>());

        act.Should().NotThrow();
    }

    [Fact]
    public void GetEntityListOptions_HasInt32MaxLimitAndZeroOffsetByDefault()
    {
        var options = new GetEntityListOptions<FakeEntity>();

        options.Offset.Should().Be(0);
        options.Limit.Should().Be(int.MaxValue);
        options.Order.Should().BeNull();
    }

    [Fact]
    public void OrderBy_SetsAscendingOrderForTheGivenProperty()
    {
        var options = new GetEntityListOptions<FakeEntity>();

        var result = options.OrderBy(e => e.Name);

        result.Should().BeSameAs(options.Order);
        var entries = options.Order!.Enumerate().ToList();
        entries.Should().ContainSingle().Which.Direction.Should().Be(1);
    }

    [Fact]
    public void OrderByDescending_SetsDescendingOrderForTheGivenProperty()
    {
        var options = new GetEntityListOptions<FakeEntity>();

        options.OrderByDescending(e => e.Name);

        options.Order!.Enumerate().Single().Direction.Should().Be(-1);
    }

    [Fact]
    public void ThenBy_AppendsAscendingOrderEntry()
    {
        var options = new GetEntityListOptions<FakeEntity>();

        options.OrderBy(e => e.Name).ThenBy(e => e.Id);

        options.Order!.Enumerate().Select(o => o.Direction).Should().Equal(1, 1);
    }

    [Fact]
    public void ThenByDescending_AppendsDescendingOrderEntry()
    {
        var options = new GetEntityListOptions<FakeEntity>();

        options.OrderBy(e => e.Name).ThenByDescending(e => e.Id);

        options.Order!.Enumerate().Select(o => o.Direction).Should().Equal(1, -1);
    }

    private sealed class FakeEntity : IPlatformEntity
    {
        public string Id { get; set; } = "id";
        public int Revision { get; set; }
        public string Name { get; set; } = "name";
    }
}
