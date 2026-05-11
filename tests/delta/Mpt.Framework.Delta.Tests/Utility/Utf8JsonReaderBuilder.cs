using System.Text;
using System.Text.Json;

namespace Mpt.Framework.Delta.Tests.Utility;

internal static class Utf8JsonReaderBuilder
{
    internal static Utf8JsonReader GetJsonReader(string contents)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(contents));
        while (reader.TokenType == JsonTokenType.None)
        {
            if (!reader.Read())
            {
                break;
            }
        }

        return reader;
    }
}
