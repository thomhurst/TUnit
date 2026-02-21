namespace TUnit.Mocks.Exceptions;

/// <summary>
/// Thrown when a mock verification fails because the actual call count
/// did not match the expected <see cref="Times"/> constraint.
/// </summary>
public class MockVerificationException : Exception
{
    /// <summary>A description of the expected call signature including argument matchers.</summary>
    public string ExpectedCall { get; }

    /// <summary>The <see cref="Times"/> constraint that was expected.</summary>
    public Times ExpectedTimes { get; }

    /// <summary>The actual number of matching calls that were recorded.</summary>
    public int ActualCount { get; }

    /// <summary>Formatted descriptions of all actual calls to the verified member.</summary>
    public IReadOnlyList<string> ActualCalls { get; }

    /// <summary>
    /// Initializes a new instance with full verification failure details.
    /// </summary>
    /// <param name="expectedCall">Description of the expected call signature.</param>
    /// <param name="expectedTimes">The expected call count constraint.</param>
    /// <param name="actualCount">The actual number of matching calls.</param>
    /// <param name="actualCalls">Formatted descriptions of all actual calls.</param>
    public MockVerificationException(string expectedCall, Times expectedTimes, int actualCount, IReadOnlyList<string> actualCalls, string? customMessage = null)
        : base(FormatMessage(expectedCall, expectedTimes, actualCount, actualCalls, customMessage))
    {
        ExpectedCall = expectedCall;
        ExpectedTimes = expectedTimes;
        ActualCount = actualCount;
        ActualCalls = actualCalls;
    }

    /// <summary>
    /// Initializes a new instance for ordered verification failures with a pre-formatted message.
    /// </summary>
    /// <param name="message">The pre-formatted error message.</param>
    public MockVerificationException(string message)
        : base(message)
    {
        ExpectedCall = string.Empty;
        ExpectedTimes = Times.Never;
        ActualCount = 0;
        ActualCalls = Array.Empty<string>();
    }

    private static string FormatMessage(string expectedCall, Times expectedTimes, int actualCount, IReadOnlyList<string> actualCalls, string? customMessage = null)
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            sb.AppendLine(customMessage);
        }
        sb.AppendLine($"Mock verification failed.");
        sb.AppendLine($"  Expected: {expectedCall} to be called {expectedTimes}");
        sb.AppendLine($"  Actual:   called {actualCount} time(s)");
        if (actualCalls.Count > 0)
        {
            sb.AppendLine("  Actual calls:");
            foreach (var call in actualCalls)
            {
                sb.AppendLine($"    - {call}");
            }
        }
        return sb.ToString();
    }
}
