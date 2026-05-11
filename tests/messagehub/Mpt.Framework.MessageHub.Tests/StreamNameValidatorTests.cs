using FluentAssertions;

namespace Mpt.Framework.MessageHub.Tests;

public class StreamNameValidatorTests
{
    [Theory]
    [InlineData("validName123")]
    [InlineData("valid.name-123")]
    [InlineData("valid_name-123")]
    public void Validate_AllowedCharacters_DoesNotThrow(string validName)
    {
        var act = () => StreamNameValidator.Validate(validName, nameof(validName));

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_NullOrWhiteSpace_ThrowsArgumentException(string? invalidName)
    {
        var act = () => StreamNameValidator.Validate(invalidName!, nameof(invalidName));

        act.Should().Throw<ArgumentException>()
            .WithMessage($"Stream name cannot be null or empty (Parameter '{nameof(invalidName)}')");
    }

    [Fact]
    public void Validate_NameLongerThan255Chars_ThrowsArgumentException()
    {
        var longName = new string('a', 256);

        var act = () => StreamNameValidator.Validate(longName, nameof(longName));

        act.Should().Throw<ArgumentException>()
            .WithMessage($"Stream name cannot be longer than 255 characters (Parameter '{nameof(longName)}')");
    }

    [Fact]
    public void Validate_NameExactly255Chars_DoesNotThrow()
    {
        var name = new string('a', 255);

        var act = () => StreamNameValidator.Validate(name, nameof(name));

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("invalid name")]
    [InlineData("invalid@name")]
    [InlineData("invalid#name")]
    [InlineData("invalid/name")]
    public void Validate_DisallowedCharacters_ThrowsArgumentException(string invalidName)
    {
        var act = () => StreamNameValidator.Validate(invalidName, nameof(invalidName));

        act.Should().Throw<ArgumentException>()
            .WithMessage($"Stream name '{invalidName}' is invalid.*letters, numbers, periods (.), hyphens (-), and underscores (_)*");
    }
}
