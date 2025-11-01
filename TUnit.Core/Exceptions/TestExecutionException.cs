namespace TUnit.Core.Exceptions;

public class TestExecutionException : TUnitException
{
    public Exception? TestException { get; }
    public IReadOnlyList<Exception> HookExceptions { get; }
    public IReadOnlyList<Exception> EventReceiverExceptions { get; }

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
                var messages = string.Join("; ", hookExceptions.Select(e => e.Message));
                parts.Add($"Multiple after hooks failed: {messages}");
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
                var messages = string.Join("; ", eventReceiverExceptions.Select(e => e.Message));
                parts.Add($"{eventReceiverExceptions.Count} test end event receivers failed: {messages}");
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
