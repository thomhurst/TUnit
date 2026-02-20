namespace TUnit.Mock.Exceptions;

/// <summary>
/// Thrown when a mock configured with <see cref="MockBehavior.Strict"/> receives
/// a call that has no matching setup.
/// </summary>
public class MockStrictBehaviorException : Exception
{
    /// <summary>A formatted description of the call that had no matching setup.</summary>
    public string UnconfiguredCall { get; }

    /// <summary>
    /// Initializes a new instance for the given unconfigured call.
    /// </summary>
    /// <param name="unconfiguredCall">A formatted description of the unconfigured call.</param>
    public MockStrictBehaviorException(string unconfiguredCall)
        : base($"Strict mock behavior violation. No setup configured for: {unconfiguredCall}")
    {
        UnconfiguredCall = unconfiguredCall;
    }
}
