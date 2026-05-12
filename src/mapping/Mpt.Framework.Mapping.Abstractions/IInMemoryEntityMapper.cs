namespace Mpt.Framework.Mapping;

/// <summary>
/// Marker variant of <see cref="IDynamicEntityMapper"/> resolved when callers want the
/// in-memory mapping behaviour — references are walked recursively, platform-object
/// collections are updated in place rather than reassigned, and no persistence side
/// effects are triggered.
/// </summary>
public interface IInMemoryEntityMapper : IDynamicEntityMapper;
