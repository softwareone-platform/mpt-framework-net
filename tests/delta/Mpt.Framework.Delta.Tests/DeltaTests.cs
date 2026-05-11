using Mpt.Framework.Delta.Tests.Utility;

namespace Mpt.Framework.Delta.Tests;

public class DeltaTests
{
    // --- ctor / state ---

    [Fact]
    public void Constructor_WithoutNode_IsNotDefined()
    {
        var delta = new Delta<TestUser>(null);

        delta.IsDefined.Should().BeFalse();
        delta.Path.Should().BeEmpty();
        delta.Data.Should().BeNull();
    }

    [Fact]
    public void Constructor_PreservesExplicitPath()
    {
        var delta = new Delta<TestUser>(null, "users[3]");

        delta.Path.Should().Be("users[3]");
    }

    // --- TryGet ---

    [Fact]
    public void TryGet_OnDefinedProperty_ReturnsTrueWithValue()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}""");

        delta.TryGet(u => u.Name, out var name).Should().BeTrue();
        name.Should().Be("Alice");
    }

    [Fact]
    public void TryGet_OnAbsentProperty_ReturnsFalseAndDefault()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}""");

        delta.TryGet(u => u.Address, out var address).Should().BeFalse();
        address.Should().BeNull();
    }

    [Fact]
    public void TryGet_OnExplicitNull_ReturnsTrueWithNull()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":null}""");

        delta.TryGet(u => u.Name, out var name).Should().BeTrue();
        name.Should().BeNull();
    }

    // --- TryGetDelta / GetDelta path generation ---

    [Fact]
    public void TryGetDelta_OnAbsentProperty_StillPopulatesPath()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("{}");

        delta.TryGetDelta(u => u.Name, out var nameDelta).Should().BeFalse();
        nameDelta.Path.Should().Be("name");
    }

    [Fact]
    public void GetDelta_OnNestedProperty_BuildsCompoundPath()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"address":{"city":"NYC"}}""");

        var cityDelta = delta.GetDelta(u => u.Address!.City);

        cityDelta.IsDefined.Should().BeTrue();
        cityDelta.Path.Should().Be("address.city");
        cityDelta.Data.Should().Be("NYC");
    }

    [Fact]
    public void GetDelta_OnPropertyWithJsonPropertyName_UsesJsonName()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"email_address":"a@b.c"}""");

        var emailDelta = delta.GetDelta(u => u.Email);

        emailDelta.Path.Should().Be("email_address");
        emailDelta.Data.Should().Be("a@b.c");
    }

    [Fact]
    public void GetDelta_OnMethodCallExpression_ThrowsNotImplementedException()
    {
        // Path resolution only supports member expressions. Method calls inside the lambda
        // (e.g. via FluentAssertions' As<T>()) must throw rather than silently misbehave.
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}""");

        Assert.Throws<NotImplementedException>(() =>
            delta.TryGetDelta(u => u.Address!.As<TestAddress>().City, out _));
    }

    // --- AssignIfDefined ---

    [Fact]
    public void AssignIfDefined_WhenPropertyDefined_InvokesSetter()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":"Alice"}""");
        string? captured = "untouched";

        var assigned = delta.AssignIfDefined(u => u.Name, v => captured = v);

        assigned.Should().BeTrue();
        captured.Should().Be("Alice");
    }

    [Fact]
    public void AssignIfDefined_WhenPropertyAbsent_DoesNotInvokeSetter()
    {
        var delta = DeltaBuilder.FromJson<TestUser>("{}");
        var called = false;

        var assigned = delta.AssignIfDefined(u => u.Name, _ => called = true);

        assigned.Should().BeFalse();
        called.Should().BeFalse();
    }

    [Fact]
    public void AssignIfDefined_OnExplicitNull_InvokesSetterWithNull()
    {
        // PATCH semantic: client clears a field by sending null; setter must run.
        var delta = DeltaBuilder.FromJson<TestUser>("""{"name":null}""");
        string? captured = "untouched";

        var assigned = delta.AssignIfDefined(u => u.Name, v => captured = v);

        assigned.Should().BeTrue();
        captured.Should().BeNull();
    }

    // --- MapTo ---

    [Fact]
    public void MapTo_OnUndefinedSource_ReturnsUndefinedTarget()
    {
        var source = new Delta<TestUser>(null);

        var target = source.MapTo<TestAddress>();

        target.IsDefined.Should().BeFalse();
        target.Data.Should().BeNull();
    }

    [Fact]
    public void MapTo_PreservesMatchingPropertiesAndDropsExtras()
    {
        var source = DeltaBuilder.FromObject<TestUser>(new
        {
            name = "Alice",
            address = new { street = "Main", city = "NYC" }
        });

        var mapped = source.MapTo<TestAddressOnly>();

        mapped.IsDefined.Should().BeTrue();
        mapped.TryGetDelta(t => t.Address, out var addressDelta).Should().BeTrue();
        addressDelta.TryGet(a => a.City, out var city).Should().BeTrue();
        city.Should().Be("NYC");

        // "name" exists on source but not target — silently dropped (no extra properties on target).
        mapped.Data!.Address!.Street.Should().Be("Main");
    }

    [Fact]
    public void MapTo_OnDefinedNodeWithoutData_PreservesShapeButTargetDataIsDefault()
    {
        // Manually construct: defined node, but no data attached. MapTo should still return
        // a defined delta (so callers can introspect structure), with Data == default.
        var node = new DeltaObjectNode("root");
        var source = new Delta<TestUser>(node);

        var target = source.MapTo<TestAddress>();

        target.IsDefined.Should().BeTrue();
        target.Data.Should().BeNull();
    }

    private class TestAddressOnly
    {
        public TestAddress? Address { get; set; }
    }
}
