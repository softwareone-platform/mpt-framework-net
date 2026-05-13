using FluentAssertions;
using Mpt.Framework.Operation.Configuration;

namespace Mpt.Framework.Operation.Tests.Configuration;

public class StaticEntityNameFormatterTests
{
    [Fact]
    public void FormatEntityName_ReturnsConfiguredNameIgnoringTypeArgument()
    {
        var formatter = new StaticEntityNameFormatter("invoice");

        formatter.FormatEntityName<object>().Should().Be("invoice");
        formatter.FormatEntityName<StaticEntityNameFormatterTests>().Should().Be("invoice");
        formatter.FormatEntityName<int>().Should().Be("invoice");
    }
}
