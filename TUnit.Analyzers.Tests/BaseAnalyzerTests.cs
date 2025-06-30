namespace TUnit.Analyzers.Tests;

public class BaseAnalyzerTests
{
    protected static string GetTimeoutAttribute(bool isRequested)
    {
        if (!isRequested)
        {
            return string.Empty;
        }

        return "[Timeout(30_000)]";
    }

    protected static string GetTimeoutCancellationTokenParameter(bool isRequested)
    {
        if (!isRequested)
        {
            return string.Empty;
        }

        return ", CancellationToken token";
    }
}
