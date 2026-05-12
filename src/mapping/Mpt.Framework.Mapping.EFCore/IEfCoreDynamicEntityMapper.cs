namespace Mpt.Framework.Mapping;

/// <summary>
/// Marker variant of <see cref="IDynamicEntityMapper"/> resolved when callers want the
/// EF Core mapping behaviour — platform-entity references are reassigned by id against
/// the underlying <c>DbContext</c>, navigation collections are loaded on demand, and
/// removed items are tracked through EF Core's change tracker so they will be deleted
/// on <c>SaveChanges</c>.
/// </summary>
public interface IEfCoreDynamicEntityMapper : IDynamicEntityMapper;
