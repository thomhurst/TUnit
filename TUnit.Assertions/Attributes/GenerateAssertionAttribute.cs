namespace TUnit.Assertions;


// ReSharper disable once UnusedTypeParameter
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GenerateAssertionAttribute<TBase>(
    AssertionType type,
    string methodName,
    string? messageFactoryMethodName = null,
    string? expectationExpression = null    
) : Attribute {
    public AssertionType Type { get; } = type;
    public string MethodName { get; } = methodName;
    public string? MessageFactoryMethodName { get; } = messageFactoryMethodName;
    public string? ExpectationExpression { get; } = expectationExpression;
}