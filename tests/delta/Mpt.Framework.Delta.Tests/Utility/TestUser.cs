using System.Text.Json.Serialization;

namespace Mpt.Framework.Delta.Tests.Utility;

// JsonPropertyName attributes pin every property to its lowercase JSON name so the
// model deserializes correctly under the default JsonSerializerOptions used by
// DeltaBuilder when no explicit options are passed.
internal class TestUser
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email_address")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public TestAddress? Address { get; set; }

    [JsonPropertyName("tags")]
    public List<TestTag>? Tags { get; set; }
}

internal class TestAddress
{
    [JsonPropertyName("street")]
    public string? Street { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }
}

internal class TestTag
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
