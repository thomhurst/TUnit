using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

#pragma warning disable TPEXP

namespace TUnit.Engine.Reporters;

/// <summary>
/// Carries the history of failed retry attempts on the final <see cref="TestNode"/> update so
/// reporters (the HTML report) can render retry/flaky information. The engine emits only one
/// update per test (the final result), so without this property the per-attempt history — which
/// lives transiently on the <see cref="TestContext"/> during execution — would be unavailable
/// to consumers that work purely off <c>TestNodeUpdateMessage</c>.
/// </summary>
/// <remarks>
/// This is an engine-internal transport, not a public extensibility contract. The same data is
/// exposed publicly to in-process consumers via <c>ITestExecution.RetryAttempts</c> (reachable
/// through <c>TestContext</c>); out-of-process reporters that only see <c>TestNodeUpdateMessage</c>
/// should not depend on this property type, as it may change without notice.
/// </remarks>
internal sealed record TUnitRetryAttemptsProperty(IReadOnlyList<TestResult> Attempts) : IProperty;
