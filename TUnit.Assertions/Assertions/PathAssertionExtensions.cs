using System;
using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<string>( typeof(Path), nameof(Path.IsPathRooted), CustomName = "IsRootedPath")]
[CreateAssertion<string>( typeof(Path), nameof(Path.IsPathRooted), CustomName = "IsNotRootedPath", NegateLogic = true)]
public static partial class PathAssertionExtensions;
