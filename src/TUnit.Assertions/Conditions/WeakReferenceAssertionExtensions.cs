using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for WeakReference type using [AssertionFrom&lt;WeakReference&gt;] attributes.
/// Each assertion wraps a property from the WeakReference class.
/// </summary>
[AssertionFrom<WeakReference>(nameof(WeakReference.IsAlive), ExpectationMessage = "be alive")]
[AssertionFrom<WeakReference>(nameof(WeakReference.IsAlive), CustomName = "IsNotAlive", NegateLogic = true, ExpectationMessage = "be alive")]

[AssertionFrom<WeakReference>(nameof(WeakReference.TrackResurrection), ExpectationMessage = "track resurrection")]
[AssertionFrom<WeakReference>(nameof(WeakReference.TrackResurrection), CustomName = "DoesNotTrackResurrection", NegateLogic = true, ExpectationMessage = "track resurrection")]
public static partial class WeakReferenceAssertionExtensions
{
}
