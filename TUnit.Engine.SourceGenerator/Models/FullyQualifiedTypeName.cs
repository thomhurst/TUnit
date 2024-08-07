namespace TUnit.Engine.SourceGenerator.Models;

public record FullyQualifiedTypeName
{
    private readonly string _fullyQualifiedType;

    public FullyQualifiedTypeName(string fullyQualifiedType)
    {
        _fullyQualifiedType = fullyQualifiedType;
    }

    public string WithoutGlobalPrefix => _fullyQualifiedType;
    public string WithGlobalPrefix => $"global::{WithoutGlobalPrefix}";
    
    public static implicit operator FullyQualifiedTypeName(Type type) => new(GetNameWithoutGenericArity(type));
    
    private static string GetNameWithoutGenericArity(Type t)
    {
        var name = t.FullName!;
        var index = name.IndexOf('`');
        return index == -1 ? name : name[..index];
    }
}