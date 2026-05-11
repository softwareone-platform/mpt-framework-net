using System.Diagnostics.CodeAnalysis;

namespace Mpt.Framework.MessageHub.Internal;

internal static class StreamRoutingHelper
{
    public static IEnumerable<(string key, string value)> GetOutputAttributes(EventMessage message)
    {
        if (message.Routing == null)
            throw new ArgumentNullException(nameof(message));

        return GetOutputAttributesIterator(message);
    }

    private static IEnumerable<(string key, string value)> GetOutputAttributesIterator(EventMessage message)
    {
        yield return (MessageHubHeaders.StreamType, message.Routing.Stream.ToString());
        yield return (MessageHubHeaders.SourceModule, message.Routing.SourceModule);
        yield return (MessageHubHeaders.Entity, message.Routing.Entity);
        yield return (MessageHubHeaders.Event, message.Routing.Event);

        if (message.Routing.TargetModules.Count > 0)
            yield return (MessageHubHeaders.TargetModules, EncodeTargetModules(message.Routing.TargetModules));
    }

    public static string BuildInputFilter(string moduleName, InputStream stream)
    {
        var filter = stream.Filter;

        if (stream.State == InputStreamState.Disabling)
            return "1=0";

        var targetHeader = MessageHubHeaders.TargetModules;
        var defaultFilter = $"(({targetHeader} IS NULL) " +
            $"OR ({targetHeader} LIKE '%|{moduleName.ToLowerInvariant()}|%')" +
            $") AND {ConvertToEntityFilter(MessageHubHeaders.StreamType, GetStreamTypesAsString(stream.Sources))}";

        var conditions = new List<string> { defaultFilter };

        if (filter.Modules != null)
        {
            conditions.Add(ConvertToEntityFilter(MessageHubHeaders.SourceModule, filter.Modules));
        }
        else if (!filter.AllowOwnEvents)
        {
            conditions.Add($"{MessageHubHeaders.SourceModule} != '{moduleName}'");
        }

        if (filter.Entities != null)
            conditions.Add(ConvertToEntityFilter(MessageHubHeaders.Entity, filter.Entities));

        if (filter.Events != null)
            conditions.Add(ConvertToEntityFilter(MessageHubHeaders.Event, filter.Events));

        return string.Join(" AND ", conditions);
    }

    [ExcludeFromCodeCoverage(Justification = "Mirrors BuildInputFilter logic in-memory for the test transport")]
    public static bool ConditionSatisfied(string moduleName, EventMessage message, InputStream stream)
    {
        var filter = stream.Filter;
        var route = message.Routing;

        if (stream.State == InputStreamState.Disabling)
            return false;

        var targets = route.TargetModules;
        var shouldHandle =
            (targets.Count == 0 || targets.Exists(t => t.Equals(stream.Name, StringComparison.OrdinalIgnoreCase)))
            && stream.Sources.HasFlag(route.Stream);

        if (filter.Modules != null)
        {
            shouldHandle &= filter.Modules.Contains(route.SourceModule);
        }
        else if (!filter.AllowOwnEvents)
        {
            shouldHandle &= route.SourceModule != moduleName;
        }

        if (filter.Entities != null)
            shouldHandle &= filter.Entities.Contains(route.Entity);

        if (filter.Events != null)
            shouldHandle &= filter.Events.Contains(route.Event);

        return shouldHandle;
    }

    internal static string EncodeTargetModules(List<string> modules) =>
        "|" + string.Join("|", modules.Select(m => m.ToLowerInvariant())) + "|";

    private static string ConvertToEntityFilter(string key, IEnumerable<string> values)
    {
        var inList = string.Join(", ", values.Select(s => $"'{s}'"));
        return $"{key} IN ({inList})";
    }

    private static IEnumerable<string> GetStreamTypesAsString(StreamTypes streamType)
    {
        return Enum.GetValues<StreamTypes>()
            .Where(t => streamType.HasFlag(t) && t != StreamTypes.None)
            .Select(s => s.ToString());
    }
}
