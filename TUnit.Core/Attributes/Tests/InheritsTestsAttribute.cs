namespace TUnit.Core;

/// <summary>
/// Marks a class as inheriting test methods from its base classes.
/// </summary>
/// <remarks>
/// This attribute indicates to the TUnit test runner that test methods defined in base classes 
/// should be considered part of the derived class's test suite.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class InheritsTestsAttribute : TUnitAttribute;