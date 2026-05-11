using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests;

public class InputStreamTests
{
    [Fact]
    public void Ctor_StreamTypesNone_Throws()
    {
        var act = () => new InputStream<TestConsumer>("test", "orders", StreamTypes.None);

        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one source must be specified*");
    }

    [Fact]
    public void Ctor_InvalidProviderKey_ThrowsViaStreamNameValidator()
    {
        var act = () => new InputStream<TestConsumer>("invalid key", "orders", StreamTypes.Events);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Stream name 'invalid key' is invalid.*");
    }

    [Fact]
    public void Ctor_InvalidStreamName_ThrowsViaStreamNameValidator()
    {
        var act = () => new InputStream<TestConsumer>("test", "invalid name", StreamTypes.Events);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Stream name 'invalid name' is invalid.*");
    }

    [Fact]
    public void Ctor_CombinedStreamTypes_IsAllowed()
    {
        // The ctor only rejects None; combinations are valid since downstream filtering uses
        // HasFlag-style matching.
        var act = () => new InputStream<TestConsumer>("test", "orders", StreamTypes.Events | StreamTypes.Sync);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetFullPath_LowercasesProviderModuleAndName()
    {
        var stream = new InputStream<TestConsumer>("Test", "Orders", StreamTypes.Events);

        stream.GetFullPath("MyModule").Should().Be("mymodule.p-test.orders");
    }

    [Fact]
    public void GetProviderPath_StaticHelperLowercasesAndJoinsModuleProvider()
    {
        InputStream.GetProviderPath("MyModule", "Main").Should().Be("mymodule.p-main");
    }

    [Fact]
    public void GetProviderlessPath_StaticHelperLowercasesModuleWithStandardSuffix()
    {
        InputStream.GetProviderlessPath("MyModule").Should().Be("mymodule.p-");
    }

    [Fact]
    public void Filter_And_Settings_AreInitializedByDefault()
    {
        var stream = new InputStream<TestConsumer>("test", "orders", StreamTypes.Events);

        stream.Filter.Should().NotBeNull();
        stream.Settings.Should().NotBeNull();
    }

    private class TestConsumer { }
}
