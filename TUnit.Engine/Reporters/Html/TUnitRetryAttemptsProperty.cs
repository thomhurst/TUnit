using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

#pragma warning disable TPEXP

namespace TUnit.Engine.Reporters.Html;

/// <summary>
/// Carries the history of failed retry attempts on the final <see cref="TestNode"/> update so
/// reporters (the HTML report) can render retry/flaky information. The engine emits only one
/// update per test (the final result), so without this property the per-attempt history — which
/// lives transiently on the <see cref="TestContext"/> during execution — would be unavailable
/// to consumers that work purely off <c>TestNodeUpdateMessage</c>.
/// </summary>
internal sealed record TUnitRetryAttemptsProperty(RetryAttemptRecord[] Attempts) : IProperty;
