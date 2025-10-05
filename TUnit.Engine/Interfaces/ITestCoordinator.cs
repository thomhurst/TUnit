using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Interface for executing a single test
/// </summary>
internal interface ITestCoordinator
{
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Test execution involves reflection for hooks and initialization")]
    #endif
    Task ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken);
}
