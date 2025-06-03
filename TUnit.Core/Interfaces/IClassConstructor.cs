using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a constructor for test classes, allowing custom instantiation strategies.
/// </summary>
public interface IClassConstructor
{
    /// <summary>
    /// Creates an instance of the specified type using custom instantiation logic.
    /// </summary>
    /// <param name="type">The type to instantiate. Must have accessible public constructors.</param>
    /// <param name="classConstructorMetadata">Metadata containing the test session ID and builder context for the test being executed.</param>
    /// <returns>
    /// A new instance of the specified type. The implementation is responsible for resolving constructor
    /// parameters and dependencies.
    /// </returns>
    /// <remarks>
    /// This method is called by the test framework to create instances of test classes.
    /// It allows for custom dependency injection or specialized test class instantiation.
    /// Implementations can use the provided <see cref="TestBuilderContext"/> from the <paramref name="classConstructorMetadata"/>
    /// to access shared data and event subscriptions for the current test execution.
    /// </remarks>
    object Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata);
}