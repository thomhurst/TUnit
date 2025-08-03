using System.Runtime.CompilerServices;

namespace TUnit.Core;

#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterEveryAttribute(HookType hookType, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) : HookAttribute(hookType, file, line);
