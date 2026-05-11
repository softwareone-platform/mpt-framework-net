using System.Globalization;

namespace Mpt.Framework.MessageHub;

/// <summary>
/// Declarative consumer registration: a named subscription on a module's outbound topic
/// that routes matching <see cref="EventMessage"/> instances to a consumer type. Concrete
/// instances are produced by <see cref="InputStreamProvider.DefineStream{TConsumer}"/>.
/// </summary>
public abstract class InputStream
{
    protected InputStream(string providerKey, string name, StreamTypes sources)
    {
        StreamNameValidator.Validate(providerKey, nameof(providerKey));
        StreamNameValidator.Validate(name, nameof(name));

        if (sources == StreamTypes.None)
            throw new ArgumentException("At least one source must be specified", nameof(sources));

        Provider = providerKey;
        Name = name;
        Sources = sources;
    }

    public string Provider { get; }

    public string Name { get; }

    public StreamTypes Sources { get; }

    public virtual InputStreamState State { get; set; }

    public abstract Type ConsumerType { get; }

    public InputStreamFilter Filter { get; set; } = new InputStreamFilter();

    public InputStreamSettings Settings { get; set; } = new InputStreamSettings();

    public string GetFullPath(string moduleName) =>
        $"{GetProviderPath(moduleName, Provider)}.{Name}".ToLower(CultureInfo.CurrentCulture);

    public static string GetProviderPath(string moduleName, string provider) =>
        $"{GetProviderlessPath(moduleName)}{provider}".ToLower(CultureInfo.CurrentCulture);

    public static string GetProviderlessPath(string moduleName) =>
        $"{moduleName}.p-".ToLower(CultureInfo.CurrentCulture);
}
