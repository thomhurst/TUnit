#pragma warning disable CS9113 // Parameter is unread - Used for source generator

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DisplayNameAttribute(string displayName) : TUnitAttribute;
