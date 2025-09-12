using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Enum.IsDefined is not a boolean-returning instance method, so we can't generate assertions for it
// Enum.HasFlag is an instance method that returns boolean
[CreateAssertion(typeof(Enum), nameof(Enum.HasFlag))]
[CreateAssertion(typeof(Enum), nameof(Enum.HasFlag), CustomName = "DoesNotHaveFlag", NegateLogic = true)]
public static partial class EnumAssertionExtensions;
