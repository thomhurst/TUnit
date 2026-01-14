namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// Primitive representation of a hook method.
/// Contains only strings and primitives - no Roslyn symbols.
/// This enables proper incremental caching in the source generator.
/// </summary>
public sealed class HookModel : IEquatable<HookModel>
{
    // Identity
    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string Namespace { get; init; }
    public required string AssemblyName { get; init; }
    public required string MethodName { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }

    // Hook configuration (using strings to match existing generator API)
    public required string HookKind { get; init; } // "Before", "After", "BeforeEvery", "AfterEvery"
    public required string HookType { get; init; } // "Test", "Class", "Assembly", "TestSession", "TestDiscovery"
    public required int Order { get; init; }
    public required string? HookExecutorTypeName { get; init; }

    // Method info
    public required bool IsStatic { get; init; }
    public required bool IsAsync { get; init; }
    public required bool ReturnsVoid { get; init; }
    public required string ReturnType { get; init; }
    public required int ParameterCount { get; init; }
    public required bool HasCancellationTokenOnly { get; init; }
    public required bool HasContextOnly { get; init; }
    public required bool HasContextAndCancellationToken { get; init; }
    public required string? FirstParameterTypeName { get; init; }
    public required EquatableArray<ParameterModel> Parameters { get; init; }

    // Class info
    public required bool ClassIsGenericType { get; init; }
    public required bool ClassIsOpenGeneric { get; init; }
    public required EquatableArray<string> ClassTypeParameters { get; init; }

    // For method info generation
    public required string MethodInfoExpression { get; init; }

    // Attributes
    public required ExtractedAttribute HookAttribute { get; init; }
    public required EquatableArray<ExtractedAttribute> MethodAttributes { get; init; }

    public bool Equals(HookModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FullyQualifiedTypeName == other.FullyQualifiedTypeName
               && MethodName == other.MethodName
               && HookKind == other.HookKind
               && HookType == other.HookType
               && Order == other.Order
               && ParameterCount == other.ParameterCount
               && IsStatic == other.IsStatic;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as HookModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = FullyQualifiedTypeName.GetHashCode();
            hash = (hash * 397) ^ MethodName.GetHashCode();
            hash = (hash * 397) ^ HookKind.GetHashCode();
            hash = (hash * 397) ^ HookType.GetHashCode();
            hash = (hash * 397) ^ Order;
            hash = (hash * 397) ^ ParameterCount;
            hash = (hash * 397) ^ IsStatic.GetHashCode();
            return hash;
        }
    }
}
