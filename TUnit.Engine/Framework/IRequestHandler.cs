using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine.Framework;

/// <summary>
/// Handles different types of test execution requests
/// </summary>
internal interface IRequestHandler
{
    Task HandleRequestAsync(TestExecutionRequest request, ITestDiscoveryServices discoveryServices, ITestExecutionServices executionServices, ILoggingServices loggingServices, ExecuteRequestContext context, ITestExecutionFilter? testExecutionFilter);
}
