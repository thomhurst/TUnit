using System;
using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Properties
[CreateAssertion(typeof(Uri), nameof(Uri.IsAbsoluteUri))]
[CreateAssertion(typeof(Uri), nameof(Uri.IsAbsoluteUri), CustomName = "IsNotAbsoluteUri", NegateLogic = true)]
[CreateAssertion(typeof(Uri), nameof(Uri.IsDefaultPort))]
[CreateAssertion(typeof(Uri), nameof(Uri.IsDefaultPort), CustomName = "IsNotDefaultPort", NegateLogic = true)]
[CreateAssertion(typeof(Uri), nameof(Uri.IsFile))]
[CreateAssertion(typeof(Uri), nameof(Uri.IsFile), CustomName = "IsNotFile", NegateLogic = true)]
[CreateAssertion(typeof(Uri), nameof(Uri.IsLoopback))]
[CreateAssertion(typeof(Uri), nameof(Uri.IsLoopback), CustomName = "IsNotLoopback", NegateLogic = true)]
[CreateAssertion(typeof(Uri), nameof(Uri.IsUnc))]
[CreateAssertion(typeof(Uri), nameof(Uri.IsUnc), CustomName = "IsNotUnc", NegateLogic = true)]

// Instance methods
[CreateAssertion(typeof(Uri), nameof(Uri.IsBaseOf))]
[CreateAssertion(typeof(Uri), nameof(Uri.IsBaseOf), CustomName = "IsNotBaseOf", NegateLogic = true)]
[CreateAssertion(typeof(Uri), nameof(Uri.IsWellFormedOriginalString))]
[CreateAssertion(typeof(Uri), nameof(Uri.IsWellFormedOriginalString), CustomName = "IsNotWellFormedOriginalString", NegateLogic = true)]

// Static methods
[CreateAssertion(typeof(string), typeof(Uri), nameof(Uri.IsWellFormedUriString))]
[CreateAssertion(typeof(string), typeof(Uri), nameof(Uri.IsWellFormedUriString), CustomName = "IsNotWellFormedUriString", NegateLogic = true)]
[CreateAssertion(typeof(char), typeof(Uri), nameof(Uri.IsHexDigit))]
[CreateAssertion(typeof(char), typeof(Uri), nameof(Uri.IsHexDigit), CustomName = "IsNotHexDigit", NegateLogic = true)]
[CreateAssertion(typeof(string), typeof(Uri), nameof(Uri.IsHexEncoding))]
[CreateAssertion(typeof(string), typeof(Uri), nameof(Uri.IsHexEncoding), CustomName = "IsNotHexEncoding", NegateLogic = true)]
public static partial class UriAssertionExtensions;
