// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace TUnit.Assertions;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
// ReSharper disable once UnusedTypeParameter
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GenerateIsAssertionAttribute<TBase>(string methodName) : Attribute {
    public string MethodName { get; } = methodName;
}

// ReSharper disable once UnusedTypeParameter
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GenerateIsNotAssertionAttribute<TBase>(string methodName) : Attribute {
    public string MethodName { get; } = methodName;

}