using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Assertions;

[CreateAssertion(typeof(string), nameof(string.StartsWith), CustomName = "StartsWith")]
[CreateAssertion(typeof(string), nameof(string.EndsWith), CustomName = "EndsWith")]
[CreateAssertion(typeof(string), nameof(string.StartsWith), CustomName = "DoesNotStartWith", NegateLogic = true)]
[CreateAssertion(typeof(string), nameof(string.EndsWith), CustomName = "DoesNotEndWith", NegateLogic = true)]
[CreateAssertion(typeof(string), nameof(string.Contains), CustomName = "DoesNotContain", NegateLogic = true)]
public static partial class StringAssertionExtensions;
