using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;

namespace TUnit.Engine.Framework;

/// <summary>
/// Handles different types of test execution requests
/// </summary>
internal interface IRequestHandler
{
    #if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Generic test instantiation requires MakeGenericType")]
    #endif
    Task HandleRequestAsync(TestExecutionRequest request, TUnitServiceProvider serviceProvider, ExecuteRequestContext context, ITestExecutionFilter? testExecutionFilter);
}
