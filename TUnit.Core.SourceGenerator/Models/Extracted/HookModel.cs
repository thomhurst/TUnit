namespace TUnit.Core.SourceGenerator.Models.Extracted;

/// <summary>
/// The scope level of a hook.
/// </summary>
public enum HookLevel
{
    Assembly,
    Class,
    Test
}

/// <summary>
/// The type of hook (before or after).
/// </summary>
public enum HookType
{
    Before,
    After
}

/// <summary>
/// The timing of the hook (each invocation or every test in scope).
/// </summary>
public enum HookTiming
{
    Each,
    Every
}

/// <summary>
/// Primitive representation of a hook method.
/// Contains only strings and primitives - no Roslyn symbols.
/// </summary>
public sealed class HookModel : IEquatable<HookModel>
{
    // Identity
    public required string FullyQualifiedTypeName { get; init; }
    public required string MinimalTypeName { get; init; }
    public required string Namespace { get; init; }
    public required string MethodName { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }

    // Hook configuration
    public required HookLevel Level { get; init; }
    public required HookType Type { get; init; }
    public required HookTiming Timing { get; init; }
    public required int Order { get; init; }
    public required string? HookExecutorTypeName { get; init; }

    // Method info
    public required bool IsStatic { get; init; }
    public required bool IsAsync { get; init; }
    public required bool ReturnsVoid { get; init; }
    public required EquatableArray<string> ParameterTypes { get; init; }
    public required EquatableArray<ParameterModel> Parameters { get; init; }

    // Class info
    public required bool ClassIsStatic { get; init; }
    public required EquatableArray<string> ClassTypeParameters { get; init; }

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
               && Level == other.Level
               && Type == other.Type
               && Timing == other.Timing
               && Order == other.Order
               && ParameterTypes.Equals(other.ParameterTypes);
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
            hash = (hash * 397) ^ (int)Level;
            hash = (hash * 397) ^ (int)Type;
            hash = (hash * 397) ^ (int)Timing;
            hash = (hash * 397) ^ Order;
            hash = (hash * 397) ^ ParameterTypes.GetHashCode();
            return hash;
        }
    }
}
