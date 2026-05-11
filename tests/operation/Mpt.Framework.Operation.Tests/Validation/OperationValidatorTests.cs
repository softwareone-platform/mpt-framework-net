using Mpt.Framework.Operation.Utility;

namespace Mpt.Framework.Operation.Tests.Validation;

public class OperationValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ValidateName_ShouldThrowArgumentException_WhenNameIsNullOrWhiteSpace(string? name)
    {
        // Arrange
        var argumentName = "name";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => OperationValidator.ValidateName(name!, argumentName));
        Assert.Equal("The operation name cannot be null or empty (Parameter 'name')", exception.Message);
        Assert.Equal(argumentName, exception.ParamName);
    }

    [Fact]
    public void ValidateName_ShouldThrowArgumentException_WhenNameIsTooLong()
    {
        // Arrange
        var name = new string('a', 256);
        var argumentName = "name";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => OperationValidator.ValidateName(name, argumentName));
        Assert.Equal("The operation name cannot be longer than 255 characters (Parameter 'name')", exception.Message);
        Assert.Equal(argumentName, exception.ParamName);
    }

    [Theory]
    [InlineData("valid.name")]
    [InlineData("another.valid.name")]
    public void ValidateName_ShouldNotThrowException_WhenNameIsValid(string name)
    {
        // Arrange
        var argumentName = "name";

        // Act & Assert
        var exception = Record.Exception(() => OperationValidator.ValidateName(name, argumentName));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("InvalidName")]
    [InlineData("invalid_name")]
    [InlineData("invalid name")]
    [InlineData("invalid@name")]
    public void ValidateName_ShouldThrowArgumentException_WhenNameIsInvalid(string name)
    {
        // Arrange
        var argumentName = "name";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => OperationValidator.ValidateName(name, argumentName));
        Assert.StartsWith("The operation name '", exception.Message);
        Assert.Contains("is invalid. It can only contain lowercase letters, periods (.) and dashes (-)", exception.Message);
        Assert.Equal(argumentName, exception.ParamName);
    }
}
