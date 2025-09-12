using System;
using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion(typeof(string), typeof(Path), nameof(Path.IsPathRooted), CustomName = "IsRooted")]
[CreateAssertion(typeof(string), typeof(Path), nameof(Path.IsPathRooted), CustomName = "IsNotRooted", NegateLogic = true)]
public static partial class PathAssertionExtensions;
