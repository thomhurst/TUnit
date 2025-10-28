using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for ValueTask type using [AssertionFrom&lt;ValueTask&gt;] attributes.
/// Each assertion wraps a property from the ValueTask structure.
/// </summary>
[AssertionFrom<ValueTask>(nameof(ValueTask.IsCompleted), ExpectationMessage = "be completed")]
[AssertionFrom<ValueTask>(nameof(ValueTask.IsCompleted), CustomName = "IsNotCompleted", NegateLogic = true, ExpectationMessage = "be completed")]

[AssertionFrom<ValueTask>(nameof(ValueTask.IsCanceled), ExpectationMessage = "be canceled")]
[AssertionFrom<ValueTask>(nameof(ValueTask.IsCanceled), CustomName = "IsNotCanceled", NegateLogic = true, ExpectationMessage = "be canceled")]

[AssertionFrom<ValueTask>(nameof(ValueTask.IsFaulted), ExpectationMessage = "be faulted")]
[AssertionFrom<ValueTask>(nameof(ValueTask.IsFaulted), CustomName = "IsNotFaulted", NegateLogic = true, ExpectationMessage = "be faulted")]

[AssertionFrom<ValueTask>(nameof(ValueTask.IsCompletedSuccessfully), ExpectationMessage = "be completed successfully")]
[AssertionFrom<ValueTask>(nameof(ValueTask.IsCompletedSuccessfully), CustomName = "IsNotCompletedSuccessfully", NegateLogic = true, ExpectationMessage = "be completed successfully")]
public static partial class ValueTaskAssertionExtensions
{
}
