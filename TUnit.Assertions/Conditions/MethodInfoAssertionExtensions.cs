using System.Reflection;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for MethodInfo.
/// </summary>
public static partial class MethodInfoAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be public", InlineMethodBody = true)]
    public static bool IsPublic(this MethodInfo value) => value.IsPublic;

    [GenerateAssertion(ExpectationMessage = "to not be public", InlineMethodBody = true)]
    public static bool IsNotPublic(this MethodInfo value) => !value.IsPublic;

    [GenerateAssertion(ExpectationMessage = "to be private", InlineMethodBody = true)]
    public static bool IsPrivate(this MethodInfo value) => value.IsPrivate;

    [GenerateAssertion(ExpectationMessage = "to not be private", InlineMethodBody = true)]
    public static bool IsNotPrivate(this MethodInfo value) => !value.IsPrivate;

    [GenerateAssertion(ExpectationMessage = "to be static", InlineMethodBody = true)]
    public static bool IsStatic(this MethodInfo value) => value.IsStatic;

    [GenerateAssertion(ExpectationMessage = "to not be static", InlineMethodBody = true)]
    public static bool IsNotStatic(this MethodInfo value) => !value.IsStatic;

    [GenerateAssertion(ExpectationMessage = "to be virtual", InlineMethodBody = true)]
    public static bool IsVirtual(this MethodInfo value) => value.IsVirtual;

    [GenerateAssertion(ExpectationMessage = "to not be virtual", InlineMethodBody = true)]
    public static bool IsNotVirtual(this MethodInfo value) => !value.IsVirtual;

    [GenerateAssertion(ExpectationMessage = "to be abstract", InlineMethodBody = true)]
    public static bool IsAbstract(this MethodInfo value) => value.IsAbstract;

    [GenerateAssertion(ExpectationMessage = "to not be abstract", InlineMethodBody = true)]
    public static bool IsNotAbstract(this MethodInfo value) => !value.IsAbstract;

    [GenerateAssertion(ExpectationMessage = "to be final (sealed)", InlineMethodBody = true)]
    public static bool IsFinal(this MethodInfo value) => value.IsFinal;

    [GenerateAssertion(ExpectationMessage = "to not be final (sealed)", InlineMethodBody = true)]
    public static bool IsNotFinal(this MethodInfo value) => !value.IsFinal;

    [GenerateAssertion(ExpectationMessage = "to be a generic method", InlineMethodBody = true)]
    public static bool IsGenericMethod(this MethodInfo value) => value.IsGenericMethod;

    [GenerateAssertion(ExpectationMessage = "to not be a generic method", InlineMethodBody = true)]
    public static bool IsNotGenericMethod(this MethodInfo value) => !value.IsGenericMethod;

    [GenerateAssertion(ExpectationMessage = "to be internal", InlineMethodBody = true)]
    public static bool IsInternal(this MethodInfo value) => value.IsAssembly;

    [GenerateAssertion(ExpectationMessage = "to not be internal", InlineMethodBody = true)]
    public static bool IsNotInternal(this MethodInfo value) => !value.IsAssembly;

    [GenerateAssertion(ExpectationMessage = "to be protected", InlineMethodBody = true)]
    public static bool IsProtected(this MethodInfo value) => value.IsFamily;

    [GenerateAssertion(ExpectationMessage = "to not be protected", InlineMethodBody = true)]
    public static bool IsNotProtected(this MethodInfo value) => !value.IsFamily;

    [GenerateAssertion(ExpectationMessage = "to return type {expectedType}")]
    public static AssertionResult ReturnsType(this MethodInfo value, Type expectedType)
    {
        if (value.ReturnType == expectedType)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"returns {value.ReturnType.Name}");
    }

    [GenerateAssertion(ExpectationMessage = "to have {expectedCount} parameters")]
    public static AssertionResult HasParameterCount(this MethodInfo value, int expectedCount)
    {
        var actualCount = value.GetParameters().Length;
        if (actualCount == expectedCount)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has {actualCount} parameters");
    }

    [GenerateAssertion(ExpectationMessage = "to have attribute of type {attributeType}")]
    public static AssertionResult HasAttribute(this MethodInfo value, Type attributeType)
    {
        if (value.GetCustomAttribute(attributeType) != null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"does not have attribute {attributeType.Name}");
    }

    [GenerateAssertion(ExpectationMessage = "to not have attribute of type {attributeType}")]
    public static AssertionResult DoesNotHaveAttribute(this MethodInfo value, Type attributeType)
    {
        if (value.GetCustomAttribute(attributeType) == null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has attribute {attributeType.Name}");
    }

    [GenerateAssertion(ExpectationMessage = "to have name '{expectedName}'")]
    public static AssertionResult HasName(this MethodInfo value, string expectedName)
    {
        if (value.Name == expectedName)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has name '{value.Name}'");
    }
}
