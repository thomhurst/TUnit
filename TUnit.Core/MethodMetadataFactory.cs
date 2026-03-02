using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Factory for creating MethodMetadata instances.
/// Replaces inline <c>new MethodMetadata { ... }</c> object initializers in generated code,
/// reducing per-method IL size and JIT-compiled native code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MethodMetadataFactory
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067",
        Justification = "Factory is only called from generated code that always passes concrete types")]
    public static MethodMetadata Create(
        string name,
        Type type,
        Type returnType,
        ClassMetadata classMetadata,
        int genericTypeCount = 0,
        ParameterMetadata[]? parameters = null)
    {
        return new MethodMetadata
        {
            Name = name,
            Type = type,
            TypeInfo = new ConcreteType(type),
            GenericTypeCount = genericTypeCount,
            ReturnType = returnType,
            ReturnTypeInfo = new ConcreteType(returnType),
            Parameters = parameters ?? [],
            Class = classMetadata,
        };
    }
}
