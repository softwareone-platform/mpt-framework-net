using System.Text.Json;
using Mpt.Framework.Delta.Tests.Utility;

namespace Mpt.Framework.Delta.Tests;

public class DeltaJsonConverterFactoryTests
{
    [Theory]
    [InlineData(typeof(Delta<string>), true)]
    [InlineData(typeof(Delta<int>), true)]
    [InlineData(typeof(Delta<TestUser>), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(List<string>), false)]
    [InlineData(typeof(Dictionary<string, int>), false)]
    public void CanConvert_ReturnsTrueOnlyForDeltaGenerics(Type type, bool expected)
    {
        new DeltaJsonConverterFactory().CanConvert(type).Should().Be(expected);
    }

    [Fact]
    public void CreateConverter_ReturnsConverterTypedToInnerArgument()
    {
        var factory = new DeltaJsonConverterFactory();

        var converter = factory.CreateConverter(typeof(Delta<TestUser>), JsonSerializerOptions.Default);

        converter.Should().BeOfType<DeltaJsonConverter<TestUser>>();
    }
}
