using System.Text.Json;
using Mpt.Framework.Delta.Tests.Utility;

namespace Mpt.Framework.Delta.Tests;

public class DeltaJsonConverterTests
{
    private static JsonSerializerOptions WithFactory()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DeltaJsonConverterFactory());
        return options;
    }

    [Fact]
    public void Deserialize_PopulatesDeltaFromJson()
    {
        var delta = JsonSerializer.Deserialize<Delta<TestUser>>("""{"name":"Alice"}""", WithFactory());

        delta!.IsDefined.Should().BeTrue();
        delta.TryGet(u => u.Name, out var name).Should().BeTrue();
        name.Should().Be("Alice");
    }

    [Fact]
    public void Deserialize_DistinguishesAbsentNullAndValue()
    {
        // The whole reason Delta<T> exists. A single payload mixes all three states;
        // each property's Delta must reflect the right one.
        var delta = JsonSerializer.Deserialize<Delta<TestUser>>(
            """{"name":"Alice","email_address":null}""",
            WithFactory());

        // Present with value:
        delta!.TryGetDelta(u => u.Name, out var name).Should().BeTrue();
        name.Data.Should().Be("Alice");

        // Present with null:
        delta.TryGetDelta(u => u.Email, out var email).Should().BeTrue();
        email.IsDefined.Should().BeTrue();
        email.Data.Should().BeNull();

        // Absent:
        delta.TryGetDelta(u => u.Address, out var address).Should().BeFalse();
        address.IsDefined.Should().BeFalse();
    }

    [Fact]
    public void Deserialize_HonorsJsonPropertyNameOnNestedTypes()
    {
        var delta = JsonSerializer.Deserialize<Delta<TestUser>>(
            """{"email_address":"a@b.c"}""",
            WithFactory());

        delta!.TryGet(u => u.Email, out var email).Should().BeTrue();
        email.Should().Be("a@b.c");
    }

    [Fact]
    public void Deserialize_OnNestedObjects_PreservesDefinedNessOfChildren()
    {
        var delta = JsonSerializer.Deserialize<Delta<TestUser>>(
            """{"address":{"city":"NYC"}}""",
            WithFactory());

        delta!.TryGetDelta(u => u.Address, out var address).Should().BeTrue();
        address.TryGetDelta(a => a.City, out var city).Should().BeTrue();
        city.Data.Should().Be("NYC");

        // Street wasn't in the JSON, even though Address was:
        address.TryGetDelta(a => a.Street, out _).Should().BeFalse();
    }

    [Fact]
    public void Write_Throws()
    {
        // Writing a Delta<T> isn't supported — these are inbound-only types.
        var converter = new DeltaJsonConverter<TestUser>();

        Assert.Throws<NotImplementedException>(() =>
            converter.Write(default!, default!, JsonSerializerOptions.Default));
    }
}
