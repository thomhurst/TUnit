using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1327;

// Fixtures for the IDE (Microsoft.Testing.Platform server-mode) exception reporting regressions
// driven from TUnit.RpcTests. Every test here is expected to fail.
[EngineTest(ExpectedResult.Failure)]
public class IdeExceptionReportingTests
{
    [Test]
    public void AggregateFailures()
    {
        var failures = new List<Exception>();

        foreach (var action in new Action[] { First, Second })
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                failures.Add(e);
            }
        }

        throw new AggregateException("Two failures", failures);
    }

    [Test]
    [Timeout(50)]
    public async Task Timeout_With_Nested_Diagnostic(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            await Task.Delay(50);
            throw new TaskCanceledException(
                "Custom task cancellation diagnostic",
                new InvalidOperationException("Inner diagnostic", new FormatException("Root cause")),
                ex.CancellationToken);
        }
    }

    private static void First()
    {
        throw new InvalidOperationException("First failure");
    }

    private static void Second()
    {
        try
        {
            throw new FormatException("Second failure cause");
        }
        catch (Exception e)
        {
            throw new ArgumentException("Second failure", e);
        }
    }
}
