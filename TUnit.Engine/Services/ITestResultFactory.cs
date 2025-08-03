using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Factory for creating test results
/// </summary>
internal interface ITestResultFactory
{
    TestResult CreatePassedResult(DateTimeOffset startTime);
    TestResult CreateFailedResult(DateTimeOffset startTime, Exception exception);
    TestResult CreateSkippedResult(DateTimeOffset startTime, string reason);
    TestResult CreateTimeoutResult(DateTimeOffset startTime, int timeoutMs);
}
