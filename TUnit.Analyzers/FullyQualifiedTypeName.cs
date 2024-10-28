namespace TUnit.Analyzers;

public record FullyQualifiedTypeName
{
    public FullyQualifiedTypeName(string fullyQualifiedType)
    {
        WithoutGlobalPrefix = fullyQualifiedType;
        WithGlobalPrefix = $"global::{WithoutGlobalPrefix}";
    }

    public string WithoutGlobalPrefix { get; }

    public string WithGlobalPrefix { get; }
}