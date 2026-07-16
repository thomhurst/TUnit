using System.Diagnostics;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Process type using [AssertionFrom&lt;Process&gt;] attributes.
/// Each assertion wraps a property from the Process class.
/// </summary>
[AssertionFrom<Process>(nameof(Process.HasExited), ExpectationMessage = "have exited")]
[AssertionFrom<Process>(nameof(Process.HasExited), CustomName = "HasNotExited", NegateLogic = true, ExpectationMessage = "have exited")]

[AssertionFrom<Process>(nameof(Process.Responding), ExpectationMessage = "be responding")]
[AssertionFrom<Process>(nameof(Process.Responding), CustomName = "IsNotResponding", NegateLogic = true, ExpectationMessage = "be responding")]

[AssertionFrom<Process>(nameof(Process.EnableRaisingEvents), ExpectationMessage = "have event raising enabled")]
[AssertionFrom<Process>(nameof(Process.EnableRaisingEvents), CustomName = "DoesNotHaveEventRaisingEnabled", NegateLogic = true, ExpectationMessage = "have event raising enabled")]
public static partial class ProcessAssertionExtensions
{
}
