namespace TUnit.Assertions.Core;

/// <summary>
/// Represents the result of an assertion check.
/// </summary>
public readonly struct AssertionResult
{
    internal static readonly Task<AssertionResult> _passedTask = Task.FromResult(Passed);

    public bool IsPassed { get; }
    public string Message { get; }

    private AssertionResult(bool isPassed, string message)
    {
        IsPassed = isPassed;
        Message = message ?? string.Empty;
    }

    /// <summary>
    /// Creates a passing assertion result.
    /// </summary>
    public static AssertionResult Passed => new(true, string.Empty);

    /// <summary>
    /// Creates a failing assertion result with a message describing what was found.
    /// </summary>
    public static AssertionResult Failed(string message) => new(false, message);

    /// <summary>
    /// Helper method to conditionally create a failed result.
    /// Returns Passed if condition is false, Failed with message if condition is true.
    /// </summary>
    public static AssertionResult FailIf(bool condition, string message)
        => condition ? Failed(message) : Passed;
}
