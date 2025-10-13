using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Task type using [AssertionFrom&lt;Task&gt;] attributes.
/// Each assertion wraps a property from the Task class.
/// </summary>
[AssertionFrom<Task>(nameof(Task.IsCompleted), ExpectationMessage = "be completed")]
[AssertionFrom<Task>(nameof(Task.IsCompleted), CustomName = "IsNotCompleted", NegateLogic = true, ExpectationMessage = "be completed")]

[AssertionFrom<Task>(nameof(Task.IsCanceled), ExpectationMessage = "be canceled")]
[AssertionFrom<Task>(nameof(Task.IsCanceled), CustomName = "IsNotCanceled", NegateLogic = true, ExpectationMessage = "be canceled")]

[AssertionFrom<Task>(nameof(Task.IsFaulted), ExpectationMessage = "be faulted")]
[AssertionFrom<Task>(nameof(Task.IsFaulted), CustomName = "IsNotFaulted", NegateLogic = true, ExpectationMessage = "be faulted")]

#if NET6_0_OR_GREATER
[AssertionFrom<Task>(nameof(Task.IsCompletedSuccessfully), ExpectationMessage = "be completed successfully")]
[AssertionFrom<Task>(nameof(Task.IsCompletedSuccessfully), CustomName = "IsNotCompletedSuccessfully", NegateLogic = true, ExpectationMessage = "be completed successfully")]
#endif
public static partial class TaskAssertionExtensions
{
}
