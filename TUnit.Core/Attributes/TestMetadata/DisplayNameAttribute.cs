namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DisplayNameAttribute(string DisplayName) : TUnitAttribute;