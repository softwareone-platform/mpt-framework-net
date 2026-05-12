namespace Mpt.Framework.Persistence;

/// <summary>
/// Thrown by repository / query <c>GetOrThrow</c> overloads when no entity exists for
/// the requested identifier.
/// </summary>
public class PersistenceEntityNotFoundException(object id) : Exception($"Entity for given id {id} not found")
{
    /// <summary>
    /// Gets the identifier that was requested.
    /// </summary>
    public object Id { get; } = id;
}
