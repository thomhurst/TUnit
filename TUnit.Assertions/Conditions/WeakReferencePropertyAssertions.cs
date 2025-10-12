using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for WeakReference type using [AssertionFrom&lt;WeakReference&gt;] attributes.
/// Each assertion wraps a property from the WeakReference class.
/// </summary>
[AssertionFrom<WeakReference>("IsAlive", ExpectationMessage = "be alive")]
[AssertionFrom<WeakReference>("IsAlive", CustomName = "IsDead", NegateLogic = true, ExpectationMessage = "be alive")]
public static partial class WeakReferencePropertyAssertions
{
}
