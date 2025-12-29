using System.Xml.Linq;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions.Xml;

/// <summary>
/// Source-generated assertions for XAttribute types.
/// </summary>
public static partial class XAttributeAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to have name '{expectedName}'")]
    public static AssertionResult HasName(this XAttribute value, string expectedName)
    {
        if (value.Name.LocalName == expectedName)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has name '{value.Name.LocalName}'");
    }

    [GenerateAssertion(ExpectationMessage = "to have value '{expectedValue}'")]
    public static AssertionResult HasValue(this XAttribute value, string expectedValue)
    {
        if (value.Value == expectedValue)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has value '{value.Value}'");
    }

    [GenerateAssertion(ExpectationMessage = "to be a namespace declaration")]
    public static AssertionResult IsNamespaceDeclaration(this XAttribute value)
    {
        if (value.IsNamespaceDeclaration)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("is not a namespace declaration");
    }

    [GenerateAssertion(ExpectationMessage = "to not be a namespace declaration")]
    public static AssertionResult IsNotNamespaceDeclaration(this XAttribute value)
    {
        if (!value.IsNamespaceDeclaration)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("is a namespace declaration");
    }
}
