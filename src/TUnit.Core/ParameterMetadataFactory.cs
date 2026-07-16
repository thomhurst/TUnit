using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

/// <summary>
/// Factory for creating ParameterMetadata instances.
/// Replaces inline <c>new ParameterMetadata { ... }</c> object initializers in generated code,
/// reducing per-parameter IL size and JIT-compiled native code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ParameterMetadataFactory
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067",
        Justification = "Factory is only called from generated code that always passes concrete types")]
    public static ParameterMetadata Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.NonPublicConstructors
            | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type,
        string name,
        TypeInfo typeInfo,
        bool isNullable,
        Func<ParameterInfo>? reflectionInfoFactory = null)
    {
        return new ParameterMetadata(type)
        {
            Name = name,
            TypeInfo = typeInfo,
            IsNullable = isNullable,
            ReflectionInfoFactory = reflectionInfoFactory,
        };
    }
}
