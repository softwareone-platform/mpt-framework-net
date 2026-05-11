using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mpt.Framework.Operations.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mpt.Framework.Operations.EntityFrameworkCore;

[ExcludeFromCodeCoverage(Justification = "Configuration")]
public class OperationSagaEntityConfiguration : IEntityTypeConfiguration<OperationSaga>
{
    private static readonly JsonSerializerOptions _jsonOptions = BuildJsonOptions();

    private readonly List<(Type, string)>? _children;

    public OperationSagaEntityConfiguration() { }

    public OperationSagaEntityConfiguration(List<(Type, string)> children) : this()
    {
        _children = children;
    }

    public void Configure(EntityTypeBuilder<OperationSaga> builder)
    {
        builder.ToTable("Utils.Operations");
        builder.HasKey(o => new { o.CorrelationId, o.Type });
        builder.Property(o => o.CorrelationId).HasColumnName("Id");

        builder.Property(t => t.Status).HasMaxLength(50);
        builder.OwnsOne(t => t.Timestamps);
        builder.OwnsOne(t => t.Statistics);
        builder.OwnsOne(t => t.StartCondition);
        builder.Property(t => t.Version).IsConcurrencyToken();

        builder.Property(t => t.Data)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, _jsonOptions),
                v => v == null ? null : JsonSerializer.Deserialize<JsonObject>(v, _jsonOptions)!)
            .HasColumnType("nvarchar(max)");

        builder.OwnsOne(t => t.Failure, f =>
        {
            f.Property(p => p.Type)
                .HasConversion(v => v.ToString(), v => Enum.Parse<OperationFailureType>(v))
                .HasMaxLength(Enum.GetNames<OperationFailureType>().Max(n => n.Length))
                .HasDefaultValue(default(OperationFailureType))
                .IsRequired();
            f.Property(p => p.Message);
        });

        builder.Property(t => t.TaskStates)
            .Metadata.SetValueComparer(new ValueComparer<byte[]?>(
                (a, b) => a == b || a != null && b != null && a.SequenceEqual(b),
                a => a == null ? 0 : a.Aggregate(0, (hash, b) => HashCode.Combine(hash, b)),
                a => a == null ? null : a.ToArray()
            ));

        if (_children != null && _children.Count > 0)
        {
            var baseType = builder.HasDiscriminator(t => t.Type);

            foreach (var (type, name) in _children)
            {
                baseType.HasValue(type, name);
            }
        }
    }

    private static JsonSerializerOptions BuildJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        return options;
    }
}
