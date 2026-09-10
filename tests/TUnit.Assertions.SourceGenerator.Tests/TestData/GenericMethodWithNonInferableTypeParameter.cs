using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>
/// Test case: Generic method where not all type parameters can be inferred
/// Simulates a Result type pattern where TError cannot be inferred from parameters
/// </summary>
public static partial class GenericMethodWithNonInferableTypeParameterExtensions
{
    /// <summary>
    /// Tests a method with two type parameters where only one can be inferred.
    /// TValue can be inferred from Result&lt;TValue&gt;, but TError cannot.
    /// </summary>
    [GenerateAssertion(ExpectationMessage = "to be Error")]
    public static bool IsErrorOfType<TValue, TError>(this Result<TValue> result) where TError : System.Exception
    {
        // Simplified version of the user's code
        return result.IsError && result.Error is TError;
    }
}

/// <summary>
/// Simplified Result type for testing
/// </summary>
public class Result<TValue>
{
    public bool IsError { get; init; }
    public System.Exception? Error { get; init; }
    public TValue? Value { get; init; }
}
