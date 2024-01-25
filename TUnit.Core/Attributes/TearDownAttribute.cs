namespace TUnit.Core.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TearDownAttribute : TUnitAttribute;