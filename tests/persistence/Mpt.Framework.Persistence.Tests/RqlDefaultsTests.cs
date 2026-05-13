using FluentAssertions;
using Mpt.Rql.Abstractions.Configuration;
using NSubstitute;

namespace Mpt.Framework.Persistence.Tests;

public class RqlDefaultsTests
{
    [Fact]
    public void EmptyRequest_HasNullFilterOrderAndSelect()
    {
        RqlDefaults.EmptyRequest.Filter.Should().BeNullOrEmpty();
        RqlDefaults.EmptyRequest.Order.Should().BeNullOrEmpty();
        RqlDefaults.EmptyRequest.Select.Should().BeNullOrEmpty();
    }

    [Fact]
    public void SetSingleItemDefaults_AppliesWithoutThrowing()
    {
        var settings = Substitute.For<IRqlSettings>();

        var act = () => RqlDefaults.SetSingleItemDefaults(settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetListDefaults_AppliesWithoutThrowing()
    {
        var settings = Substitute.For<IRqlSettings>();

        var act = () => RqlDefaults.SetListDefaults(settings);

        act.Should().NotThrow();
    }

    [Fact]
    public void InMemoryDefaults_AppliesWithoutThrowing()
    {
        var settings = Substitute.For<IRqlSettings>();

        var act = () => RqlDefaults.InMemoryDefaults(settings);

        act.Should().NotThrow();
    }
}
