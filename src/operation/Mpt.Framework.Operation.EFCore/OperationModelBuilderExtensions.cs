using Mpt.Framework.Operation.EFCore;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

public static class OperationModelBuilderExtensions
{
    /// <summary>
    /// Adds the operation saga entity (<c>Utils.Operations</c>) to the given EF Core model.
    /// Call this from your primary <c>DbContext.OnModelCreating</c>.
    /// </summary>
    public static ModelBuilder AddOperationEntity(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OperationSagaEntityConfiguration());
        return modelBuilder;
    }
}
