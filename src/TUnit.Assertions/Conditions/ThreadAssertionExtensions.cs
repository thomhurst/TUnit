using System.Threading;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Thread type using [AssertionFrom&lt;Thread&gt;] attributes.
/// Each assertion wraps a property from the Thread class.
/// </summary>
[AssertionFrom<Thread>(nameof(Thread.IsAlive), ExpectationMessage = "be alive")]
[AssertionFrom<Thread>(nameof(Thread.IsAlive), CustomName = "IsNotAlive", NegateLogic = true, ExpectationMessage = "be alive")]

[AssertionFrom<Thread>(nameof(Thread.IsBackground), ExpectationMessage = "be a background thread")]
[AssertionFrom<Thread>(nameof(Thread.IsBackground), CustomName = "IsNotBackground", NegateLogic = true, ExpectationMessage = "be a background thread")]

[AssertionFrom<Thread>(nameof(Thread.IsThreadPoolThread), ExpectationMessage = "be a thread pool thread")]
[AssertionFrom<Thread>(nameof(Thread.IsThreadPoolThread), CustomName = "IsNotThreadPoolThread", NegateLogic = true, ExpectationMessage = "be a thread pool thread")]
public static partial class ThreadAssertionExtensions
{
}
