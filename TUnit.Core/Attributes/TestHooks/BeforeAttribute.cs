using System.Runtime.CompilerServices;

#pragma warning disable CS9113

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeAttribute(HookType hookType, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : HookAttribute(hookType, file, line);
