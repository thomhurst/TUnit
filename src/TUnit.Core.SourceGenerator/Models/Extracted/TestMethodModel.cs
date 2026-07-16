namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of a test method.
/// Contains only strings and primitives - no Roslyn symbols.
/// This is the key model for incremental caching in TestMetadataGenerator.
/// </summary>
public sealed class TestMethodModel : IEquatable<TestMethodModel>
{
    // Type identity
    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string Namespace { get; init; }
    public required string AssemblyName { get; init; }

    // Method identity
    public required string MethodName { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }

    // Generics
    public required bool IsGenericType { get; init; }
    public required bool IsGenericMethod { get; init; }
    public required EquatableArray<string> TypeParameters { get; init; }
    public required EquatableArray<string> TypeParameterConstraints { get; init; }
    public required EquatableArray<string> MethodTypeParameters { get; init; }
    public required EquatableArray<string> MethodTypeParameterConstraints { get; init; }

    // Method signature
    public required string ReturnType { get; init; }
    public required bool IsAsync { get; init; }
    public required bool ReturnsVoid { get; init; }
    public required EquatableArray<ParameterModel> Parameters { get; init; }

    // Attributes
    public required ExtractedAttribute TestAttribute { get; init; }
    public required EquatableArray<ExtractedAttribute> MethodAttributes { get; init; }
    public required EquatableArray<ExtractedAttribute> ClassAttributes { get; init; }

    // Data sources
    public required EquatableArray<DataSourceModel> MethodDataSources { get; init; }
    public required EquatableArray<DataSourceModel> ClassDataSources { get; init; }
    public required EquatableArray<DataSourceModel> ParameterDataSources { get; init; }

    // Inheritance
    public required int InheritanceDepth { get; init; }

    // Class info
    public required bool ClassIsStatic { get; init; }
    public required bool ClassHasParameterlessConstructor { get; init; }
    public required EquatableArray<ParameterModel> ClassConstructorParameters { get; init; }

    // AOT types needed
    public required EquatableArray<string> TypesNeedingConverters { get; init; }

    public bool Equals(TestMethodModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Core identity comparison - these fields uniquely identify a test
        return FullyQualifiedTypeName == other.FullyQualifiedTypeName
               && MethodName == other.MethodName
               && FilePath == other.FilePath
               && LineNumber == other.LineNumber
               && IsGenericType == other.IsGenericType
               && IsGenericMethod == other.IsGenericMethod
               && InheritanceDepth == other.InheritanceDepth
               && TypeParameters.Equals(other.TypeParameters)
               && MethodTypeParameters.Equals(other.MethodTypeParameters)
               && Parameters.Equals(other.Parameters)
               && MethodDataSources.Equals(other.MethodDataSources)
               && ClassDataSources.Equals(other.ClassDataSources)
               && MethodAttributes.Equals(other.MethodAttributes);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TestMethodModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = FullyQualifiedTypeName.GetHashCode();
            hash = (hash * 397) ^ MethodName.GetHashCode();
            hash = (hash * 397) ^ FilePath.GetHashCode();
            hash = (hash * 397) ^ LineNumber;
            hash = (hash * 397) ^ IsGenericType.GetHashCode();
            hash = (hash * 397) ^ IsGenericMethod.GetHashCode();
            hash = (hash * 397) ^ InheritanceDepth;
            hash = (hash * 397) ^ TypeParameters.GetHashCode();
            hash = (hash * 397) ^ MethodTypeParameters.GetHashCode();
            return hash;
        }
    }
}
