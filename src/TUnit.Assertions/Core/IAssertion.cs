namespace TUnit.Assertions.Core;

/// <summary>
/// Defines a non-generic contract for all assertions, allowing them to be
/// executed without knowledge of their underlying generic type. This is crucial
/// for scenarios like `.Satisfies()` where type-changing assertions can occur.
/// </summary>
public interface IAssertion
{
    /// <summary>
    /// Executes the assertion logic and throws an exception if it fails.
    /// This is the type-erased version of <see cref="Assertion{TValue}.AssertAsync()"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the assertion is executed.</returns>
    Task AssertAsync();
}
