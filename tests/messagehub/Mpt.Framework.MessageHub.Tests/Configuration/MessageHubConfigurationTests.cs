using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Mpt.Framework.MessageHub.Tests.Configuration;

public class MessageHubConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AddMessageHub_NullOrEmptyModuleCode_Throws(string? moduleCode)
    {
        var services = new ServiceCollection();

        var act = () => services.AddMessageHub(moduleCode!, _ => { });

        act.Should().Throw<ArgumentException>()
            .WithMessage("Module code cannot be null or empty*");
    }

    [Fact]
    public void AddMessageHub_NullConfigureDelegate_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMessageHub("module", configure: null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMessageHub_ServiceBusTransportWithoutConnectionString_Throws()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMessageHub("module", hub =>
        {
            hub.Settings.Transport = MessageHubTransport.ServiceBus;
            // ConnectionString deliberately left null
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString is required*");
    }

    [Fact]
    public void AddMessageHub_InMemoryTransportWithoutConnectionString_Succeeds()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddMessageHub("module", hub =>
        {
            hub.Settings.Transport = MessageHubTransport.InMemory;
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void AddMessageHub_RegistersPublisherInDi()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMessageHub("module", hub =>
        {
            hub.Settings.Transport = MessageHubTransport.InMemory;
        });

        using var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IMessageHubPublisher>();

        publisher.Should().NotBeNull();
    }

    [Fact]
    public void AddMessageHub_RegistersBusInDi()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMessageHub("module", hub =>
        {
            hub.Settings.Transport = MessageHubTransport.InMemory;
        });

        using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IMessageHubBus>();

        bus.Should().NotBeNull();
    }

    [Fact]
    public void Builder_ConfigureInputByType_AddsProviderInstance()
    {
        var builder = new MessageHubBuilder(new ServiceCollection(), "module");

        builder.ConfigureInput<TestProvider>();

        builder.StreamProviders.Should().ContainSingle()
            .Which.Should().BeOfType<TestProvider>();
    }

    [Fact]
    public void Builder_ConfigureInputByInstance_AddsTheGivenInstance()
    {
        var builder = new MessageHubBuilder(new ServiceCollection(), "module");
        var instance = new TestProvider();

        builder.ConfigureInput(instance);

        builder.StreamProviders.Should().ContainSingle()
            .Which.Should().BeSameAs(instance);
    }

    private class TestProvider : InputStreamProvider
    {
        public override string Key => "test";

        public override IEnumerable<InputStream> GetInputStreams() => [];
    }
}
