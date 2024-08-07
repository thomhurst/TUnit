#pragma warning disable CS9113

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method)]
public class AfterAttribute(HookType hookType) : TUnitAttribute;