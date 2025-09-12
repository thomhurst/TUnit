using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Version comparison helpers
[CreateAssertion(typeof(Version), typeof(VersionAssertionExtensions), nameof(IsMajorVersion))]
[CreateAssertion(typeof(Version), typeof(VersionAssertionExtensions), nameof(IsMajorVersion), CustomName = "IsNotMajorVersion", NegateLogic = true)]
public static partial class VersionAssertionExtensions
{
    internal static bool IsMajorVersion(Version version) =>
        version is { Major: > 0, Minor: 0 or -1, Build: 0 or -1, Revision: 0 or -1 };
}
