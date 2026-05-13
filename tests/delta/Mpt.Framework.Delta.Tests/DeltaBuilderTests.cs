using System.Text;
using System.Text.Json;
using Mpt.Framework.Delta.Tests.Utility;

namespace Mpt.Framework.Delta.Tests;

public class DeltaBuilderTests
{
    [Fact]
    public void Empty_ReturnsDeltaWithNoNode()
    {
        var delta = DeltaBuilder.Empty<TestUser>();

        delta.IsDefined.Should().BeFalse();
        delta.Node.Should().BeNull();
        delta.Data.Should().BeNull();
        delta.Path.Should().BeEmpty();
    }

    [Fact]
    public void FromJson_OnPresentProperty_DeltaIsDefined()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}""");

        delta.TryGetDelta(u => u.Name, out var nameDelta).Should().BeTrue();
        nameDelta.Data.Should().Be("Alice");
    }

    [Fact]
    public void FromJson_OnAbsentProperty_DeltaIsNotDefined()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}""");

        delta.TryGetDelta(u => u.Address, out var addressDelta).Should().BeFalse();
        addressDelta.IsDefined.Should().BeFalse();
        addressDelta.Data.Should().BeNull();
    }

    [Fact]
    public void FromJson_OnExplicitNull_DeltaIsDefinedWithNullData()
    {
        // This is the core PATCH semantic: client sent the property with value null,
        // so we must distinguish that from "client did not send it".
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":null}""");

        delta.TryGetDelta(u => u.Name, out var nameDelta).Should().BeTrue();
        nameDelta.IsDefined.Should().BeTrue();
        nameDelta.Data.Should().BeNull();
    }

    [Fact]
    public void FromJson_OnEmptyObject_RootDeltaIsDefinedButPropertiesAreNot()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("{}");

        delta.IsDefined.Should().BeTrue();
        delta.TryGetDelta(u => u.Name, out _).Should().BeFalse();
        delta.TryGetDelta(u => u.Address, out _).Should().BeFalse();
    }

    [Fact]
    public void FromJson_HonorsJsonPropertyNameAttribute()
    {
        // TestUser.Email is mapped to "email_address" via [JsonPropertyName]
        var delta = DeltaBuilder.FromJson<TestUser>("""{"email_address":"a@b.c"}""");

        delta.TryGetDelta(u => u.Email, out var emailDelta).Should().BeTrue();
        emailDelta.Data.Should().Be("a@b.c");
        emailDelta.Path.Should().Be("email_address");
    }

    [Fact]
    public void FromJson_OnDuplicateKeys_ThrowsDeltaException()
    {
        var json = """{ "name": "a", "name": "b" }""";

        var act = () => DeltaBuilder.FromJson<TestUser>(json);

        act.Should().Throw<DeltaException>();
    }

    [Fact]
    public void FromObject_BuildsDeltaFromAnonymousType()
    {
        var delta = DeltaBuilder.FromObject<TestUser>(new { name = "Alice", address = new { city = "NYC" } });

        delta.TryGet(u => u.Name, out var name).Should().BeTrue();
        name.Should().Be("Alice");

        delta.TryGetDelta(u => u.Address, out var addressDelta).Should().BeTrue();
        addressDelta!.TryGet(a => a.City, out var city).Should().BeTrue();
        city.Should().Be("NYC");
    }

    [Fact]
    public void FromReader_AfterUtf8JsonReaderIsAdvanced_BuildsDelta()
    {
        var bytes = Encoding.UTF8.GetBytes("""{"name":"Alice"}""");
        var reader = new Utf8JsonReader(bytes);
        reader.Read();

        var delta = DeltaBuilder.FromReader<TestUser>(ref reader);

        delta.TryGet(u => u.Name, out var name).Should().BeTrue();
        name.Should().Be("Alice");
    }
}
