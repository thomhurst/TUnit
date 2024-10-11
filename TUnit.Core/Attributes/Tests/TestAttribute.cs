using System.Runtime.CompilerServices;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute(
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 0)
    : BaseTestAttribute(file, line);