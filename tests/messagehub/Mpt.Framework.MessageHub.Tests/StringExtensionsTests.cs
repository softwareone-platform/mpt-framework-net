using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("Billing", "billing")]
    [InlineData("PriceList", "priceList")]
    [InlineData("X", "x")]
    [InlineData("already", "already")]
    public void ToEventPathString_LowercasesOnlyTheFirstCharacter(string input, string expected)
    {
        input.ToEventPathString().Should().Be(expected);
    }
}
