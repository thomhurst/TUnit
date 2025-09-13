using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<string>( nameof(string.StartsWith), CustomName = "StartsWith")]
[CreateAssertion<string>( nameof(string.EndsWith), CustomName = "EndsWith")]
[CreateAssertion<string>( nameof(string.StartsWith), CustomName = "DoesNotStartWith", NegateLogic = true)]
[CreateAssertion<string>( nameof(string.EndsWith), CustomName = "DoesNotEndWith", NegateLogic = true)]
[CreateAssertion<string>( nameof(string.Contains), CustomName = "DoesNotContain", NegateLogic = true)]
public static partial class StringAssertionExtensions;
