namespace TUnit.Core.Exceptions;

/// <summary>
/// Exception thrown when one or more failures occur during test execution, including the test itself,
/// hooks (before/after test methods), or event receivers.
/// This exception aggregates multiple failure types to provide comprehensive error reporting.
/// </summary>
/// <remarks>
/// This exception is thrown in the following scenarios:
/// <list type="bullet">
/// <item><description>The test itself fails and one or more hooks or event receivers also fail</description></item>
/// <item><description>The test passes but one or more hooks or event receivers fail during cleanup</description></item>
/// <item><description>Multiple hooks fail during test execution</description></item>
/// <item><description>Multiple event receivers fail during test execution</description></item>
/// </list>
/// The <see cref="InnerException"/> property contains either a single exception or an <see cref="AggregateException"/>
/// when multiple failures occur.
/// </remarks>
public class TestExecutionException : TUnitException
{
    /// <summary>
    /// Gets the exception thrown by the test itself, or null if the test passed.
    /// </summary>
    public Exception? TestException { get; }

    /// <summary>
    /// Gets the collection of exceptions thrown by hooks during test execution.
    /// </summary>
    public IReadOnlyList<Exception> HookExceptions { get; }

    /// <summary>
    /// Gets the collection of exceptions thrown by event receivers during test execution.
    /// </summary>
    public IReadOnlyList<Exception> EventReceiverExceptions { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestExecutionException"/> class.
    /// </summary>
    /// <param name="testException">The exception thrown by the test, or null if the test passed.</param>
    /// <param name="hookExceptions">The collection of exceptions thrown by hooks.</param>
    /// <param name="eventReceiverExceptions">The collection of exceptions thrown by event receivers.</param>
    public TestExecutionException(
        Exception? testException,
        IReadOnlyList<Exception> hookExceptions,
        IReadOnlyList<Exception> eventReceiverExceptions)
        : base(BuildMessage(testException, hookExceptions, eventReceiverExceptions),
               BuildInnerException(testException, hookExceptions, eventReceiverExceptions))
    {
        TestException = testException;
        HookExceptions = hookExceptions;
        EventReceiverExceptions = eventReceiverExceptions;
    }

    private static string BuildMessage(
        Exception? testException,
        IReadOnlyList<Exception> hookExceptions,
        IReadOnlyList<Exception> eventReceiverExceptions)
    {
        var parts = new List<string>();

        if (testException is not null)
        {
            parts.Add($"Test failed: {testException.Message}");
        }

        if (hookExceptions.Count > 0)
        {
            if (hookExceptions.Count == 1)
            {
                parts.Add(hookExceptions[0].Message);
            }
            else
            {
                var messageBuilder = new System.Text.StringBuilder();
                messageBuilder.Append("Multiple hooks failed: ");
                for (var i = 0; i < hookExceptions.Count; i++)
                {
                    if (i > 0)
                    {
                        messageBuilder.Append("; ");
                    }
                    messageBuilder.Append(hookExceptions[i].Message);
                }
                parts.Add(messageBuilder.ToString());
            }
        }

        if (eventReceiverExceptions.Count > 0)
        {
            if (eventReceiverExceptions.Count == 1)
            {
                parts.Add($"Test end event receiver failed: {eventReceiverExceptions[0].Message}");
            }
            else
            {
                var messageBuilder = new System.Text.StringBuilder();
                messageBuilder.Append($"{eventReceiverExceptions.Count} test end event receivers failed: ");
                for (var i = 0; i < eventReceiverExceptions.Count; i++)
                {
                    if (i > 0)
                    {
                        messageBuilder.Append("; ");
                    }
                    messageBuilder.Append(eventReceiverExceptions[i].Message);
                }
                parts.Add(messageBuilder.ToString());
            }
        }

        return string.Join(" | ", parts);
    }

    private static Exception? BuildInnerException(
        Exception? testException,
        IReadOnlyList<Exception> hookExceptions,
        IReadOnlyList<Exception> eventReceiverExceptions)
    {
        var allExceptions = new List<Exception>();

        if (testException is not null)
        {
            allExceptions.Add(testException);
        }

        allExceptions.AddRange(hookExceptions);
        allExceptions.AddRange(eventReceiverExceptions);

        return allExceptions.Count switch
        {
            0 => null,
            1 => allExceptions[0],
            _ => new AggregateException(allExceptions)
        };
    }
}
