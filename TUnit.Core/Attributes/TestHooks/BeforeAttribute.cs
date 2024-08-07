#pragma warning disable CS9113

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method)]
public class BeforeAttribute(HookType hookType) : TUnitAttribute;