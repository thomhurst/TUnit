using System.ComponentModel;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for CancellationToken type using [AssertionFrom&lt;CancellationToken&gt;] and [GenerateAssertion] attributes.
/// </summary>
[AssertionFrom<CancellationToken>(nameof(CancellationToken.CanBeCanceled), ExpectationMessage = "be cancellable")]
[AssertionFrom<CancellationToken>(nameof(CancellationToken.CanBeCanceled), CustomName = "CannotBeCanceled", NegateLogic = true, ExpectationMessage = "be cancellable")]

[AssertionFrom<CancellationToken>(nameof(CancellationToken.IsCancellationRequested), ExpectationMessage = "have cancellation requested")]
[AssertionFrom<CancellationToken>(nameof(CancellationToken.IsCancellationRequested), CustomName = "IsNotCancellationRequested", NegateLogic = true, ExpectationMessage = "have cancellation requested")]
public static partial class CancellationTokenAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be CancellationToken.None")]
    public static bool IsNone(this CancellationToken value) => value.Equals(CancellationToken.None);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be CancellationToken.None")]
    public static bool IsNotNone(this CancellationToken value) => !value.Equals(CancellationToken.None);
}
