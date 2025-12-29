using System.Xml.Linq;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions.Xml;

/// <summary>
/// Source-generated assertions for XElement types.
/// </summary>
public static partial class XElementAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to have name '{expectedName}'")]
    public static AssertionResult HasName(this XElement value, string expectedName)
    {
        if (value.Name.LocalName == expectedName)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has name '{value.Name.LocalName}'");
    }

    [GenerateAssertion(ExpectationMessage = "to have attribute '{attributeName}'")]
    public static AssertionResult HasAttribute(this XElement value, string attributeName)
    {
        if (value.Attribute(attributeName) != null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"does not have attribute '{attributeName}'");
    }

    [GenerateAssertion(ExpectationMessage = "to not have attribute '{attributeName}'")]
    public static AssertionResult DoesNotHaveAttribute(this XElement value, string attributeName)
    {
        if (value.Attribute(attributeName) == null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has attribute '{attributeName}'");
    }

    [GenerateAssertion(ExpectationMessage = "to have attribute '{attributeName}' with value '{expectedValue}'")]
    public static AssertionResult HasAttributeValue(this XElement value, string attributeName, string expectedValue)
    {
        var attr = value.Attribute(attributeName);
        if (attr == null)
        {
            return AssertionResult.Failed($"does not have attribute '{attributeName}'");
        }

        if (attr.Value == expectedValue)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"attribute '{attributeName}' has value '{attr.Value}'");
    }

    [GenerateAssertion(ExpectationMessage = "to have child element '{childName}'")]
    public static AssertionResult HasChildElement(this XElement value, string childName)
    {
        if (value.Element(childName) != null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"does not have child element '{childName}'");
    }

    [GenerateAssertion(ExpectationMessage = "to not have child element '{childName}'")]
    public static AssertionResult DoesNotHaveChildElement(this XElement value, string childName)
    {
        if (value.Element(childName) == null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has child element '{childName}'");
    }

    [GenerateAssertion(ExpectationMessage = "to have value '{expectedValue}'")]
    public static AssertionResult HasValue(this XElement value, string expectedValue)
    {
        if (value.Value == expectedValue)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has value '{value.Value}'");
    }

    [GenerateAssertion(ExpectationMessage = "to be empty")]
    public static AssertionResult IsEmpty(this XElement value)
    {
        if (value.IsEmpty)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("is not empty");
    }

    [GenerateAssertion(ExpectationMessage = "to not be empty")]
    public static AssertionResult IsNotEmpty(this XElement value)
    {
        if (!value.IsEmpty)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("is empty");
    }

    [GenerateAssertion(ExpectationMessage = "to have namespace '{expectedNamespace}'")]
    public static AssertionResult HasNamespace(this XElement value, string expectedNamespace)
    {
        if (value.Name.NamespaceName == expectedNamespace)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has namespace '{value.Name.NamespaceName}'");
    }

    [GenerateAssertion(ExpectationMessage = "to have {expectedCount} child elements")]
    public static AssertionResult HasChildCount(this XElement value, int expectedCount)
    {
        var actualCount = value.Elements().Count();
        if (actualCount == expectedCount)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has {actualCount} child elements");
    }

    [GenerateAssertion(ExpectationMessage = "to be deeply equal to {expected}")]
    public static AssertionResult IsDeepEqualTo(this XElement value, XElement expected)
    {
        if (XNode.DeepEquals(value, expected))
        {
            return AssertionResult.Passed;
        }

        var diff = XmlDiffHelper.FindFirstDifference(value, expected);
        return AssertionResult.Failed($"differs at {diff.Path}: expected {diff.Expected} but found {diff.Actual}");
    }

    [GenerateAssertion(ExpectationMessage = "to not be deeply equal to {expected}")]
    public static AssertionResult IsNotDeepEqualTo(this XElement value, XElement expected)
    {
        if (!XNode.DeepEquals(value, expected))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("elements are equal");
    }
}
