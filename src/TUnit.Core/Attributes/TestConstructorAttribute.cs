namespace TUnit.Core;

/// <summary>
/// Marks a constructor as the preferred constructor for test class instantiation.
/// If this attribute is not present on any constructor, TUnit will use the first available constructor.
/// Only one constructor per class should be marked with this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class TestConstructorAttribute : TUnitAttribute
{
}