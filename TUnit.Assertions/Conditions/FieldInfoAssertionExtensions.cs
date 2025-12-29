using System.Reflection;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for FieldInfo.
/// </summary>
public static partial class FieldInfoAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be public", InlineMethodBody = true)]
    public static bool IsPublic(this FieldInfo value) => value.IsPublic;

    [GenerateAssertion(ExpectationMessage = "to not be public", InlineMethodBody = true)]
    public static bool IsNotPublic(this FieldInfo value) => !value.IsPublic;

    [GenerateAssertion(ExpectationMessage = "to be private", InlineMethodBody = true)]
    public static bool IsPrivate(this FieldInfo value) => value.IsPrivate;

    [GenerateAssertion(ExpectationMessage = "to not be private", InlineMethodBody = true)]
    public static bool IsNotPrivate(this FieldInfo value) => !value.IsPrivate;

    [GenerateAssertion(ExpectationMessage = "to be static", InlineMethodBody = true)]
    public static bool IsStatic(this FieldInfo value) => value.IsStatic;

    [GenerateAssertion(ExpectationMessage = "to not be static", InlineMethodBody = true)]
    public static bool IsNotStatic(this FieldInfo value) => !value.IsStatic;

    [GenerateAssertion(ExpectationMessage = "to be readonly", InlineMethodBody = true)]
    public static bool IsReadOnly(this FieldInfo value) => value.IsInitOnly;

    [GenerateAssertion(ExpectationMessage = "to not be readonly", InlineMethodBody = true)]
    public static bool IsNotReadOnly(this FieldInfo value) => !value.IsInitOnly;

    [GenerateAssertion(ExpectationMessage = "to be a constant", InlineMethodBody = true)]
    public static bool IsConstant(this FieldInfo value) => value.IsLiteral;

    [GenerateAssertion(ExpectationMessage = "to not be a constant", InlineMethodBody = true)]
    public static bool IsNotConstant(this FieldInfo value) => !value.IsLiteral;

    [GenerateAssertion(ExpectationMessage = "to be internal", InlineMethodBody = true)]
    public static bool IsInternal(this FieldInfo value) => value.IsAssembly;

    [GenerateAssertion(ExpectationMessage = "to not be internal", InlineMethodBody = true)]
    public static bool IsNotInternal(this FieldInfo value) => !value.IsAssembly;

    [GenerateAssertion(ExpectationMessage = "to be protected", InlineMethodBody = true)]
    public static bool IsProtected(this FieldInfo value) => value.IsFamily;

    [GenerateAssertion(ExpectationMessage = "to not be protected", InlineMethodBody = true)]
    public static bool IsNotProtected(this FieldInfo value) => !value.IsFamily;

    [GenerateAssertion(ExpectationMessage = "to be of type {expectedType}")]
    public static AssertionResult IsOfType(this FieldInfo value, Type expectedType)
    {
        if (value.FieldType == expectedType)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"is of type {value.FieldType.Name}");
    }

    [GenerateAssertion(ExpectationMessage = "to have attribute of type {attributeType}")]
    public static AssertionResult HasAttribute(this FieldInfo value, Type attributeType)
    {
        if (value.GetCustomAttribute(attributeType) != null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"does not have attribute {attributeType.Name}");
    }

    [GenerateAssertion(ExpectationMessage = "to not have attribute of type {attributeType}")]
    public static AssertionResult DoesNotHaveAttribute(this FieldInfo value, Type attributeType)
    {
        if (value.GetCustomAttribute(attributeType) == null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has attribute {attributeType.Name}");
    }

    [GenerateAssertion(ExpectationMessage = "to have name '{expectedName}'")]
    public static AssertionResult HasName(this FieldInfo value, string expectedName)
    {
        if (value.Name == expectedName)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has name '{value.Name}'");
    }
}
