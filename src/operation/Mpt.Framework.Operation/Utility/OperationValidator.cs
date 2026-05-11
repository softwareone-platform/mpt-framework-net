using System.Text.RegularExpressions;

namespace Mpt.Framework.Operation.Utility;

internal static partial class OperationValidator
{
    private const string PATTERN = @"^[a-z\.\-]+$";

    public static void ValidateName(string name, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The operation name cannot be null or empty", argumentName);
        }

        if (name.Length > 255)
        {
            throw new ArgumentException("The operation name cannot be longer than 255 characters", argumentName);
        }

        if (!NameRegex().IsMatch(name))
        {
            throw new ArgumentException($"The operation name '{name}' is invalid. It can only contain lowercase letters, periods (.) and dashes (-)", argumentName);
        }
    }

    [GeneratedRegex(PATTERN, RegexOptions.Compiled)]
    private static partial Regex NameRegex();
}
