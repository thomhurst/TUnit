namespace TUnit.Core.SourceGenerator.Models;

public record FullyQualifiedTypeName
{
    private readonly string _fullyQualifiedType;

    public FullyQualifiedTypeName(string fullyQualifiedType)
    {
        _fullyQualifiedType = fullyQualifiedType;
    }

    public string WithoutGlobalPrefix => _fullyQualifiedType;
    public string WithGlobalPrefix => $"global::{WithoutGlobalPrefix}";
    
    public static implicit operator FullyQualifiedTypeName(string name) => new(name);
}