namespace Mpt.Framework;

/// <summary>
/// A first-class, identifiable, revisable entity — the composition of
/// <see cref="IPlatformObject"/> and <see cref="IRevisable"/>. Framework components
/// treat platform entities as something looked up by id rather than walked into
/// property by property.
/// </summary>
public interface IPlatformEntity : IPlatformObject, IRevisable;
