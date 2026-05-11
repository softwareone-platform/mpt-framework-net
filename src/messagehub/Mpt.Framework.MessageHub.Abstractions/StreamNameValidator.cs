using System.Text.RegularExpressions;

namespace Mpt.Framework.MessageHub;

public static partial class StreamNameValidator
{
    private const string PATTERN = @"^[a-zA-Z0-9\.\-_]+$";

    public static void Validate(string name, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Stream name cannot be null or empty", argumentName);

        if (name.Length > 255)
            throw new ArgumentException("Stream name cannot be longer than 255 characters", argumentName);

        if (!NameRegex().IsMatch(name))
            throw new ArgumentException($"Stream name '{name}' is invalid. It can only contain letters, numbers, periods (.), hyphens (-), and underscores (_)", argumentName);
    }

    [GeneratedRegex(PATTERN, RegexOptions.Compiled)]
    private static partial Regex NameRegex();
}
