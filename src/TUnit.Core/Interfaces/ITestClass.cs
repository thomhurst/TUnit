using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test class-related properties.
/// Accessed via <see cref="TestDetails.Class"/>.
/// </summary>
public interface ITestClass
{
    /// <summary>
    /// Gets the type of the test class containing this test.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    Type ClassType { get; }

    /// <summary>
    /// Gets the instance of the test class.
    /// </summary>
    object ClassInstance { get; }

    /// <summary>
    /// Gets the arguments passed to the test class constructor.
    /// </summary>
    object?[] TestClassArguments { get; }

    /// <summary>
    /// Gets the property-injected arguments for the test class.
    /// </summary>
    IDictionary<string, object?> TestClassInjectedPropertyArguments { get; }

    /// <summary>
    /// Gets the parameter types for the test class constructor (may be null).
    /// </summary>
    Type[]? TestClassParameterTypes { get; }

    /// <summary>
    /// Gets the resolved generic type arguments for the test class.
    /// Will be Type.EmptyTypes if the class is not generic.
    /// </summary>
    Type[] ClassGenericArguments { get; }
}
