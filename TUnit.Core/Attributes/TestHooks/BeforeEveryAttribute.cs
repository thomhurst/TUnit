namespace TUnit.Core;

#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeEveryAttribute(HookType hookType) : HookAttribute(hookType);