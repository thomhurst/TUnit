using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for CancellationToken type using [AssertionFrom&lt;CancellationToken&gt;] attributes.
/// Each assertion wraps a property from the CancellationToken struct.
/// </summary>
[AssertionFrom<CancellationToken>("CanBeCanceled", ExpectationMessage = "be cancellable")]
[AssertionFrom<CancellationToken>("CanBeCanceled", CustomName = "CannotBeCanceled", NegateLogic = true, ExpectationMessage = "be cancellable")]

[AssertionFrom<CancellationToken>("IsCancellationRequested", ExpectationMessage = "have cancellation requested")]
[AssertionFrom<CancellationToken>("IsCancellationRequested", CustomName = "IsNotCancellationRequested", NegateLogic = true, ExpectationMessage = "have cancellation requested")]
public static partial class CancellationTokenAssertionExtensions
{
}
