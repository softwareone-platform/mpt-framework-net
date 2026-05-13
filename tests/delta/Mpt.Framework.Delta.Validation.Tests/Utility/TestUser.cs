using System.Text.Json.Serialization;

namespace Mpt.Framework.Delta.Validation.Tests.Utility;

internal class TestUser
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email_address")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public TestAddress Address { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<TestTag>? Tags { get; set; }
}

internal class TestAddress
{
    [JsonPropertyName("street")]
    public string Street { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;
}

internal class TestTag
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
