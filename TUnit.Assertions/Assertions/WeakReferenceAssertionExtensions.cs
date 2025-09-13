using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<WeakReference>( nameof(WeakReference.IsAlive))]
[CreateAssertion<WeakReference>( nameof(WeakReference.IsAlive), CustomName = "IsDead", NegateLogic = true)]

[CreateAssertion<WeakReference>( nameof(WeakReference.TrackResurrection))]
[CreateAssertion<WeakReference>( nameof(WeakReference.TrackResurrection), CustomName = "DoesNotTrackResurrection", NegateLogic = true)]

// Note: Generic WeakReference<T> would require special handling in the source generator
public static partial class WeakReferenceAssertionExtensions;