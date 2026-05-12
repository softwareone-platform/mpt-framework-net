namespace Mpt.Framework.Mapping;

internal static class TypeHelper
{
    public static string? GetPlatformEntityId(object entity)
    {
        if (entity is IPlatformObject platformObject)
        {
            return platformObject.Id;
        }

        return null;
    }

    public static bool IsPlatformObject(Type type) => typeof(IPlatformObject).IsAssignableFrom(type);

    public static bool IsPlatformEntity(Type type) => typeof(IPlatformEntity).IsAssignableFrom(type);

    public static bool IsUserComplexType(Type type)
        => type.IsClass && !type.FullName!.StartsWith("System.", StringComparison.Ordinal);
}
