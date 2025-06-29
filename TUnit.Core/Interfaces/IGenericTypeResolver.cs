using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Interface for resolving generic type parameters from runtime arguments
/// </summary>
public interface IGenericTypeResolver
{
    /// <summary>
    /// Resolves the concrete generic type arguments for a generic method based on provided runtime arguments.
    /// </summary>
    /// <param name="genericMethodDefinition">The MethodInfo for the generic method definition</param>
    /// <param name="runtimeArguments">The actual arguments passed to the test method, used for type inference</param>
    /// <returns>An array of concrete Type objects corresponding to the generic parameters</returns>
    /// <exception cref="GenericTypeResolutionException">Thrown if types cannot be resolved or constraints are violated</exception>
    Type[] ResolveGenericMethodArguments(MethodInfo genericMethodDefinition, object?[] runtimeArguments);

    /// <summary>
    /// Resolves the concrete generic type arguments for a generic type based on provided runtime arguments.
    /// </summary>
    /// <param name="genericTypeDefinition">The Type for the generic class definition</param>
    /// <param name="constructorArguments">The actual constructor arguments that might help infer types</param>
    /// <returns>An array of concrete Type objects corresponding to the generic parameters</returns>
    /// <exception cref="GenericTypeResolutionException">Thrown if types cannot be resolved or constraints are violated</exception>
    Type[] ResolveGenericClassArguments(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type genericTypeDefinition, 
        object?[] constructorArguments);
}