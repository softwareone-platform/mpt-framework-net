using Mpt.Rql;
using System.Globalization;

namespace Mpt.Framework.Persistence;

/// <summary>
/// Describes a single page of a paged query — the RQL request plus paging window
/// (<see cref="Limit"/>, <see cref="Offset"/>) and the optional total-count flag.
/// </summary>
public sealed record DataPageRequest(
    RqlRequest Request,
    int Limit,
    int Offset,
    bool CountAll,
    CustomFilters CustomFilters,
    CustomFunctions CustomFunctions)
{
    /// <summary>
    /// Convenience constructor with empty custom filters / functions.
    /// </summary>
    public DataPageRequest(RqlRequest request, int limit, int offset, bool countAll)
        : this(request, limit, offset, countAll, new CustomFilters(), new CustomFunctions())
    {
    }
}

/// <summary>
/// Collection of named custom filters parsed from the request and applied by an
/// <see cref="IFilterProvider{TDbEntity}"/>.
/// </summary>
public sealed class CustomFilters : CustomItemsContainer<CustomFilter>
{
    /// <summary>
    /// Parses a comma-separated filter name + argument list and adds the filter to the set.
    /// </summary>
    public CustomFilters Add(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var split = name.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            AddItem(new CustomFilter(split[0].ToLower(CultureInfo.InvariantCulture), split.Skip(1).ToArray()));
        }

        return this;
    }
}

/// <summary>
/// Collection of named custom functions exposed to a query.
/// </summary>
public sealed class CustomFunctions : CustomItemsContainer<CustomFunction>
{
    /// <summary>
    /// Adds a custom function by name and arguments.
    /// </summary>
    public CustomFunctions AddFunction(string functionName, string[] arguments)
    {
        if (!string.IsNullOrEmpty(functionName))
        {
            AddItem(new CustomFunction(functionName.ToLower(CultureInfo.InvariantCulture), arguments));
        }

        return this;
    }
}

/// <summary>
/// Common base for <see cref="CustomFilters"/> / <see cref="CustomFunctions"/>.
/// </summary>
public abstract class CustomItemsContainer<T> where T : CustomItem
{
    private readonly HashSet<T> _items = [];

    /// <summary>Adds an item to the container, ignoring nulls.</summary>
    protected void AddItem(T item)
    {
        if (item != null)
        {
            _items.Add(item);
        }
    }

    /// <summary>Returns the underlying set.</summary>
    public HashSet<T> Get() => _items;
}

/// <summary>A single named filter with positional arguments.</summary>
public sealed class CustomFilter(string key, string[] args) : CustomItem(key, args);

/// <summary>A single named function with positional arguments.</summary>
public sealed class CustomFunction(string name, string[] arguments) : CustomItem(name, arguments);

/// <summary>Common base for <see cref="CustomFilter"/> / <see cref="CustomFunction"/>.</summary>
public abstract class CustomItem(string key, string[] args)
{
    /// <summary>The canonical name (lower-case) the filter / function is registered under.</summary>
    public string Key { get; } = key;

    /// <summary>The positional arguments supplied.</summary>
    public string[] Args { get; } = args;
}
