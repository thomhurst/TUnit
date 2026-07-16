namespace TUnit.Core;

/// <summary>
/// Represents type information that can be encoded in source-generated code.
/// Used to bridge compile-time type knowledge and runtime type resolution,
/// particularly for generic type parameters.
/// </summary>
public abstract record TypeInfo;

/// <summary>
/// Represents a concrete type that can be referenced via typeof() at compile-time.
/// This is the most common case - used for all non-generic and closed generic types.
/// </summary>
/// <param name="Type">The actual Type object. Never null.</param>
public sealed record ConcreteType(Type Type) : TypeInfo
{
    public override string ToString() => Type.Name;
}

/// <summary>
/// Represents a generic type parameter (e.g., T, TKey, TValue) that cannot be
/// a concrete Type at source generation time. These are resolved at runtime
/// when the actual type arguments are known.
/// </summary>
/// <param name="Position">
/// The 0-based position of the generic parameter.
/// For MyClass&lt;T, U&gt;, T has position 0, U has position 1.
/// </param>
/// <param name="IsMethodParameter">
/// True if this is a method generic parameter (e.g., void Method&lt;T&gt;()),
/// false if it's a type generic parameter (e.g., class Foo&lt;T&gt;).
/// </param>
/// <param name="Name">
/// The name of the generic parameter (e.g., "T", "TKey").
/// Used for debugging and error messages. Optional.
/// </param>
public sealed record GenericParameter(
    int Position,
    bool IsMethodParameter,
    string? Name = null
) : TypeInfo
{
    public override string ToString() => Name ?? $"T{Position}";
}

/// <summary>
/// Represents a constructed generic type where some type arguments may be
/// generic parameters (e.g., List&lt;T&gt;, Dictionary&lt;string, T&gt;).
/// </summary>
/// <param name="GenericDefinition">
/// The generic type definition (e.g., typeof(List&lt;&gt;), typeof(Dictionary&lt;,&gt;)).
/// Must be a generic type definition (Type.IsGenericTypeDefinition == true).
/// </param>
/// <param name="TypeArguments">
/// The type arguments. Can be a mix of ConcreteType and GenericParameter instances.
/// For List&lt;T&gt;, this would be [new GenericParameter(0, false, "T")].
/// For Dictionary&lt;string, T&gt;, this would be [new ConcreteType(typeof(string)), new GenericParameter(0, false, "T")].
/// </param>
public sealed record ConstructedGeneric(
    Type GenericDefinition,
    TypeInfo[] TypeArguments
) : TypeInfo
{
    public override string ToString()
    {
        var name = GenericDefinition.Name;
        var backtickIndex = name.IndexOf('`');
        if (backtickIndex > 0)
        {
            name = name.Substring(0, backtickIndex);
        }
        return $"{name}<{string.Join(", ", TypeArguments.Select(t => t.ToString()))}>";
    }
}
