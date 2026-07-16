namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test method-related properties.
/// Accessed via <see cref="TestDetails.Method"/>.
/// </summary>
public interface ITestMethod
{
    /// <summary>
    /// Gets the metadata describing the test method.
    /// </summary>
    MethodMetadata MethodMetadata { get; }

    /// <summary>
    /// Gets the return type of the test method.
    /// </summary>
    Type ReturnType { get; }

    /// <summary>
    /// Gets the arguments passed to the test method.
    /// </summary>
    object?[] TestMethodArguments { get; }

    /// <summary>
    /// Gets the resolved generic type arguments for the test method.
    /// Will be Type.EmptyTypes if the method is not generic.
    /// </summary>
    Type[] MethodGenericArguments { get; }
}
