#pragma warning disable CS9113

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterAttribute(HookType hookType) : HookAttribute(hookType);