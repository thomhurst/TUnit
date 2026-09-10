using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Interface for executing a single test.
/// </summary>
/// <remarks>
/// Callers MUST invoke <see cref="ExecuteTestAsync"/> through <c>TestRunner</c>, not directly.
/// <c>TestRunner</c> owns the dedup ledger that prevents double-execution when a test is reached
/// via both the scheduler and dependency recursion. Bypassing <c>TestRunner</c> loses that
/// guarantee and can cause the same test to run twice.
/// </remarks>
internal interface ITestCoordinator
{
    ValueTask ExecuteTestAsync(AbstractExecutableTest test, CancellationToken cancellationToken);
}
