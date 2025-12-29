using System.Xml.Linq;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions.Xml;

/// <summary>
/// Source-generated assertions for XDocument types.
/// </summary>
public static partial class XDocumentAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to have a root element")]
    public static AssertionResult HasRoot(this XDocument value)
    {
        if (value.Root != null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("has no root element");
    }

    [GenerateAssertion(ExpectationMessage = "to not have a root element")]
    public static AssertionResult DoesNotHaveRoot(this XDocument value)
    {
        if (value.Root == null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has root element <{value.Root.Name.LocalName}>");
    }

    [GenerateAssertion(ExpectationMessage = "to have root element named '{expectedName}'")]
    public static AssertionResult HasRootNamed(this XDocument value, string expectedName)
    {
        if (value.Root == null)
        {
            return AssertionResult.Failed("has no root element");
        }

        if (value.Root.Name.LocalName == expectedName)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed($"has root element named '{value.Root.Name.LocalName}'");
    }

    [GenerateAssertion(ExpectationMessage = "to have an XML declaration")]
    public static AssertionResult HasDeclaration(this XDocument value)
    {
        if (value.Declaration != null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("has no XML declaration");
    }

    [GenerateAssertion(ExpectationMessage = "to not have an XML declaration")]
    public static AssertionResult DoesNotHaveDeclaration(this XDocument value)
    {
        if (value.Declaration == null)
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("has an XML declaration");
    }

    [GenerateAssertion(ExpectationMessage = "to be deeply equal to {expected}")]
    public static AssertionResult IsDeepEqualTo(this XDocument value, XDocument expected)
    {
        if (XNode.DeepEquals(value, expected))
        {
            return AssertionResult.Passed;
        }

        var diff = XmlDiffHelper.FindFirstDifference(value, expected);
        return AssertionResult.Failed($"differs at {diff.Path}: expected {diff.Expected} but found {diff.Actual}");
    }

    [GenerateAssertion(ExpectationMessage = "to not be deeply equal to {expected}")]
    public static AssertionResult IsNotDeepEqualTo(this XDocument value, XDocument expected)
    {
        if (!XNode.DeepEquals(value, expected))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Failed("documents are equal");
    }
}
