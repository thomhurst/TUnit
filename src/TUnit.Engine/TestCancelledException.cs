using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Marks a test result that TUnit reports because the test was cancelled before it finished.
/// </summary>
/// <remarks>
/// Microsoft.Testing.Platform 3 removed <c>CancelledTestNodeStateProperty</c>. TUnit reports a
/// cancelled test as an <see cref="ErrorTestNodeStateProperty"/> that carries this exception, so
/// TUnit's own reporters can still tell a cancellation apart from other errors.
/// </remarks>
internal sealed class TestCancelledException : OperationCanceledException
{
    public const string CancelledMessage = "Test was cancelled";

    public TestCancelledException() : base(CancelledMessage)
    {
    }

    public static ErrorTestNodeStateProperty CreateStateProperty() =>
        new(new TestCancelledException(), CancelledMessage);
}
