namespace Mpt.Framework.Mapping.Tests;

public class TypeHelperTests
{
    [Fact]
    public void IsPlatformObject_WithIPlatformObjectType_ReturnsTrue()
    {
        TypeHelper.IsPlatformObject(typeof(PlatformObjectEntity)).Should().BeTrue();
    }

    [Fact]
    public void IsPlatformObject_WithIPlatformEntityType_ReturnsTrue()
    {
        TypeHelper.IsPlatformObject(typeof(NamedEntity)).Should().BeTrue();
    }

    [Fact]
    public void IsPlatformObject_WithRegularClass_ReturnsFalse()
    {
        TypeHelper.IsPlatformObject(typeof(PlainEntity)).Should().BeFalse();
    }

    [Fact]
    public void IsPlatformObject_WithPrimitive_ReturnsFalse()
    {
        TypeHelper.IsPlatformObject(typeof(int)).Should().BeFalse();
    }

    [Fact]
    public void IsPlatformEntity_WithIPlatformEntityType_ReturnsTrue()
    {
        TypeHelper.IsPlatformEntity(typeof(NamedEntity)).Should().BeTrue();
    }

    [Fact]
    public void IsPlatformEntity_WithPlainIPlatformObjectType_ReturnsFalse()
    {
        TypeHelper.IsPlatformEntity(typeof(PlatformObjectEntity)).Should().BeFalse();
    }

    [Fact]
    public void GetPlatformEntityId_WithPlatformObject_ReturnsId()
    {
        TypeHelper.GetPlatformEntityId(new PlatformObjectEntity { Id = "abc" }).Should().Be("abc");
    }

    [Fact]
    public void GetPlatformEntityId_WithPlainObject_ReturnsNull()
    {
        TypeHelper.GetPlatformEntityId(new PlainEntity()).Should().BeNull();
    }

    [Fact]
    public void IsUserComplexType_WithUserClass_ReturnsTrue()
    {
        TypeHelper.IsUserComplexType(typeof(PlainEntity)).Should().BeTrue();
    }

    [Fact]
    public void IsUserComplexType_WithSystemString_ReturnsFalse()
    {
        TypeHelper.IsUserComplexType(typeof(string)).Should().BeFalse();
    }

    [Fact]
    public void IsUserComplexType_WithSystemDateTime_ReturnsFalse()
    {
        TypeHelper.IsUserComplexType(typeof(DateTime)).Should().BeFalse();
    }

    public class PlatformObjectEntity : IPlatformObject
    {
        public string Id { get; set; } = string.Empty;
    }

    public class NamedEntity : IPlatformEntity
    {
        public string Id { get; set; } = string.Empty;
        public int Revision { get; set; }
        public string? Name { get; init; }
        public string? Icon { get; init; }
    }

    public class PlainEntity
    {
        public string Name { get; set; } = string.Empty;
    }
}
