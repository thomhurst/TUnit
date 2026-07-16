namespace TUnit.Core;

/// <summary>
/// Specifies which generic type combinations should be generated for a generic test class or method.
/// This gives users explicit control over which generic instantiations are created at compile time.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class GenerateGenericTestAttribute : Attribute
{
    /// <summary>
    /// The generic type arguments to use for this instantiation
    /// </summary>
    public Type[] TypeArguments { get; }

    /// <summary>
    /// Creates a new instance specifying which generic types to instantiate
    /// </summary>
    /// <param name="typeArguments">The type arguments for the generic test</param>
    public GenerateGenericTestAttribute(params Type[] typeArguments)
    {
        TypeArguments = typeArguments ?? throw new ArgumentNullException(nameof(typeArguments));
    }
}
