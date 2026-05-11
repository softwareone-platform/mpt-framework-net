namespace Mpt.Framework.MessageHub;

public static class StringExtensions
{
    /// <summary>
    /// Lowercases the first character of <paramref name="source"/> — used to build
    /// camelCase path segments from PascalCase entity / event names.
    /// </summary>
    public static string ToEventPathString(this string source)
        => char.ToLowerInvariant(source[0]) + source[1..];
}
