using System;
using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Assertions.Extensions;

[CreateAssertion(typeof(string), typeof(Path), nameof(Path.IsPathRooted), AssertionType.Is | AssertionType.IsNot)]
public static partial class PathAssertionExtensions
{
}