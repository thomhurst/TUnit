using TUnit.Core.Exceptions;
using TUnit.Engine.Enums;

namespace TUnit.Engine.Services;

/// <summary>
/// Examines exceptions from test failures and categorizes them to help users
/// quickly understand what went wrong.
/// </summary>
internal static class FailureCategorizer
{
    /// <summary>
    /// Categorizes the given exception into a <see cref="FailureCategory"/>.
    /// Unwraps <see cref="AggregateException"/> to inspect the first inner exception.
    /// </summary>
    public static FailureCategory Categorize(Exception exception)
    {
        // Unwrap AggregateException to get the real cause
        var ex = exception is AggregateException { InnerExceptions.Count: > 0 } agg
            ? agg.InnerExceptions[0]
            : exception;

        // Setup hooks (Before*)
        if (ex is BeforeTestException
            or BeforeClassException
            or BeforeAssemblyException
            or BeforeTestSessionException
            or BeforeTestDiscoveryException)
        {
            return FailureCategory.Setup;
        }

        // Teardown hooks (After*)
        if (ex is AfterTestException
            or AfterClassException
            or AfterAssemblyException
            or AfterTestSessionException
            or AfterTestDiscoveryException)
        {
            return FailureCategory.Teardown;
        }

        // Assertion failures - check by type name to support third-party assertion libraries
        if (ex.GetType().Name.Contains("Assertion", StringComparison.Ordinal)
            || ex.GetType().Name.Contains("Assert", StringComparison.Ordinal))
        {
            return FailureCategory.Assertion;
        }

        // Timeout / cancellation
        if (ex is OperationCanceledException
            or TaskCanceledException
            or System.TimeoutException
            or TUnit.Core.Exceptions.TimeoutException)
        {
            return FailureCategory.Timeout;
        }

        // NullReference
        if (ex is NullReferenceException)
        {
            return FailureCategory.NullReference;
        }

        // Infrastructure (I/O, network, file system)
        if (ex is IOException
            or System.Net.Sockets.SocketException
            or System.Net.Http.HttpRequestException
            or UnauthorizedAccessException)
        {
            return FailureCategory.Infrastructure;
        }

        return FailureCategory.Unknown;
    }

    /// <summary>
    /// Returns a short human-readable label for the category,
    /// suitable for prefixing failure messages in reports.
    /// </summary>
    public static string GetLabel(FailureCategory category) => category switch
    {
        FailureCategory.Assertion => "Assertion Failure",
        FailureCategory.Timeout => "Timeout",
        FailureCategory.NullReference => "Null Reference",
        FailureCategory.Setup => "Setup Failure",
        FailureCategory.Teardown => "Teardown Failure",
        FailureCategory.Infrastructure => "Infrastructure Failure",
        FailureCategory.Unknown => "Test Failure",
        _ => "Test Failure"
    };
}
