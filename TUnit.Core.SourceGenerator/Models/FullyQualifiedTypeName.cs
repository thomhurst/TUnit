namespace TUnit.Core.SourceGenerator.Models;

public record FullyQualifiedTypeName
{
    public FullyQualifiedTypeName(string fullyQualifiedType)
    {
        WithoutGlobalPrefix = fullyQualifiedType;
        WithGlobalPrefix = $"global::{WithoutGlobalPrefix}";
    }

    public string WithoutGlobalPrefix { get; }

    public string WithGlobalPrefix { get; }

    public static implicit operator FullyQualifiedTypeName(string name) => new(name);
}
