using System.Reflection;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for PropertyInfo.
/// </summary>
public static partial class PropertyInfoAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be readable", InlineMethodBody = true)]
    public static bool CanRead(this PropertyInfo value) => value.CanRead;

    [GenerateAssertion(ExpectationMessage = "to not be readable", InlineMethodBody = true)]
    public static bool CannotRead(this PropertyInfo value) => !value.CanRead;

    [GenerateAssertion(ExpectationMessage = "to be writable", InlineMethodBody = true)]
    public static bool CanWrite(this PropertyInfo value) => value.CanWrite;

    [GenerateAssertion(ExpectationMessage = "to not be writable", InlineMethodBody = true)]
    public static bool CannotWrite(this PropertyInfo value) => !value.CanWrite;

    [GenerateAssertion(ExpectationMessage = "to be of type {expectedType}")]
    public static AssertionResult IsOfType(this PropertyInfo value, Type expectedType)
    {
        if (value.PropertyType == expectedType)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"is of type {value.PropertyType.Name}");
    }

    [GenerateAssertion(ExpectationMessage = "to have a getter")]
    public static AssertionResult HasGetter(this PropertyInfo value)
    {
        if (value.GetMethod != null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("has no getter");
    }

    [GenerateAssertion(ExpectationMessage = "to not have a getter")]
    public static AssertionResult DoesNotHaveGetter(this PropertyInfo value)
    {
        if (value.GetMethod == null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("has a getter");
    }

    [GenerateAssertion(ExpectationMessage = "to have a setter")]
    public static AssertionResult HasSetter(this PropertyInfo value)
    {
        if (value.SetMethod != null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("has no setter");
    }

    [GenerateAssertion(ExpectationMessage = "to not have a setter")]
    public static AssertionResult DoesNotHaveSetter(this PropertyInfo value)
    {
        if (value.SetMethod == null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("has a setter");
    }

    [GenerateAssertion(ExpectationMessage = "to be static")]
    public static AssertionResult IsStatic(this PropertyInfo value)
    {
        var isStatic = (value.GetMethod?.IsStatic ?? value.SetMethod?.IsStatic) == true;
        if (isStatic)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("is not static");
    }

    [GenerateAssertion(ExpectationMessage = "to not be static")]
    public static AssertionResult IsNotStatic(this PropertyInfo value)
    {
        var isStatic = (value.GetMethod?.IsStatic ?? value.SetMethod?.IsStatic) == true;
        if (!isStatic)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("is static");
    }

    [GenerateAssertion(ExpectationMessage = "to be public")]
    public static AssertionResult IsPublic(this PropertyInfo value)
    {
        var isPublic = (value.GetMethod?.IsPublic ?? false) || (value.SetMethod?.IsPublic ?? false);
        if (isPublic)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("is not public");
    }

    [GenerateAssertion(ExpectationMessage = "to not be public")]
    public static AssertionResult IsNotPublic(this PropertyInfo value)
    {
        var isPublic = (value.GetMethod?.IsPublic ?? false) || (value.SetMethod?.IsPublic ?? false);
        if (!isPublic)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("is public");
    }

    [GenerateAssertion(ExpectationMessage = "to have attribute of type {attributeType}")]
    public static AssertionResult HasAttribute(this PropertyInfo value, Type attributeType)
    {
        if (value.GetCustomAttribute(attributeType) != null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"does not have attribute {attributeType.Name}");
    }

    [GenerateAssertion(ExpectationMessage = "to not have attribute of type {attributeType}")]
    public static AssertionResult DoesNotHaveAttribute(this PropertyInfo value, Type attributeType)
    {
        if (value.GetCustomAttribute(attributeType) == null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has attribute {attributeType.Name}");
    }

    [GenerateAssertion(ExpectationMessage = "to have name '{expectedName}'")]
    public static AssertionResult HasName(this PropertyInfo value, string expectedName)
    {
        if (value.Name == expectedName)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has name '{value.Name}'");
    }
}
