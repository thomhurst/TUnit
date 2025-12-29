using System.Xml.Linq;

namespace TUnit.Assertions.Conditions.Xml;

/// <summary>
/// Helper class for comparing XML elements and identifying differences.
/// </summary>
internal static class XmlDiffHelper
{
    /// <summary>
    /// Represents the result of an XML comparison, including the path where a difference was found
    /// and the expected and actual values at that location.
    /// </summary>
    /// <param name="Path">The XPath-like path where the difference was found (e.g., "/root/person/@name").</param>
    /// <param name="Expected">The expected value at this path.</param>
    /// <param name="Actual">The actual value at this path.</param>
    /// <param name="HasDifference">Whether a difference was found. Defaults to true.</param>
    public readonly record struct DiffResult(string Path, string Expected, string Actual, bool HasDifference = true);

    /// <summary>
    /// Finds the first difference between two XDocument instances.
    /// </summary>
    public static DiffResult FindFirstDifference(XDocument? actual, XDocument? expected)
    {
        if (actual is null && expected is null)
        {
            return new DiffResult("/", "", "", HasDifference: false);
        }

        if (actual is null)
        {
            return new DiffResult("/", FormatNode(expected), "null");
        }

        if (expected is null)
        {
            return new DiffResult("/", "null", FormatNode(actual));
        }

        // Compare declarations
        if (actual.Declaration?.ToString() != expected.Declaration?.ToString())
        {
            return new DiffResult("/?xml",
                expected.Declaration?.ToString() ?? "(none)",
                actual.Declaration?.ToString() ?? "(none)");
        }

        // Compare roots
        if (actual.Root is null && expected.Root is null)
        {
            return new DiffResult("/", "", "", HasDifference: false);
        }

        if (actual.Root is null)
        {
            return new DiffResult("/", FormatElement(expected.Root), "(no root)");
        }

        if (expected.Root is null)
        {
            return new DiffResult("/", "(no root)", FormatElement(actual.Root));
        }

        return FindElementDiff(actual.Root, expected.Root, "");
    }

    /// <summary>
    /// Finds the first difference between two XElement instances.
    /// </summary>
    public static DiffResult FindFirstDifference(XElement? actual, XElement? expected)
    {
        if (actual is null && expected is null)
        {
            return new DiffResult("/", "", "", HasDifference: false);
        }

        if (actual is null)
        {
            return new DiffResult("/", FormatElement(expected), "null");
        }

        if (expected is null)
        {
            return new DiffResult("/", "null", FormatElement(actual));
        }

        return FindElementDiff(actual, expected, "");
    }

    private static DiffResult FindElementDiff(XElement actual, XElement expected, string parentPath)
    {
        var currentPath = $"{parentPath}/{actual.Name.LocalName}";

        // Compare element names
        if (actual.Name != expected.Name)
        {
            return new DiffResult(currentPath, $"<{expected.Name.LocalName}>", $"<{actual.Name.LocalName}>");
        }

        // Compare namespaces
        if (actual.Name.NamespaceName != expected.Name.NamespaceName)
        {
            return new DiffResult($"{currentPath}[namespace]",
                expected.Name.NamespaceName ?? "(none)",
                actual.Name.NamespaceName ?? "(none)");
        }

        // Compare attributes
        var attrDiff = CompareAttributes(actual, expected, currentPath);
        if (attrDiff.HasDifference)
        {
            return attrDiff;
        }

        // Compare text content for leaf nodes
        if (!actual.HasElements && !expected.HasElements)
        {
            if (actual.Value != expected.Value)
            {
                return new DiffResult(currentPath, Quote(expected.Value), Quote(actual.Value));
            }

            return new DiffResult(currentPath, "", "", HasDifference: false);
        }

        // Compare child elements
        var actualChildren = actual.Elements().ToList();
        var expectedChildren = expected.Elements().ToList();

        if (actualChildren.Count != expectedChildren.Count)
        {
            return new DiffResult($"{currentPath}[children]",
                $"{expectedChildren.Count} children",
                $"{actualChildren.Count} children");
        }

        for (var i = 0; i < expectedChildren.Count; i++)
        {
            var childDiff = FindElementDiff(actualChildren[i], expectedChildren[i], currentPath);
            if (childDiff.HasDifference)
            {
                return childDiff;
            }
        }

        return new DiffResult(currentPath, "", "", HasDifference: false);
    }

    private static DiffResult CompareAttributes(XElement actual, XElement expected, string elementPath)
    {
        var actualAttrs = actual.Attributes().Where(a => !a.IsNamespaceDeclaration).ToList();
        var expectedAttrs = expected.Attributes().Where(a => !a.IsNamespaceDeclaration).ToList();

        // Check for missing attributes in actual
        foreach (var expAttr in expectedAttrs)
        {
            var actualAttr = actualAttrs.FirstOrDefault(a => a.Name == expAttr.Name);
            var attrPath = $"{elementPath}/@{expAttr.Name.LocalName}";

            if (actualAttr is null)
            {
                return new DiffResult(attrPath, Quote(expAttr.Value), "(missing)");
            }

            if (actualAttr.Value != expAttr.Value)
            {
                return new DiffResult(attrPath, Quote(expAttr.Value), Quote(actualAttr.Value));
            }
        }

        // Check for extra attributes in actual
        foreach (var actAttr in actualAttrs)
        {
            var expectedAttr = expectedAttrs.FirstOrDefault(a => a.Name == actAttr.Name);
            if (expectedAttr is null)
            {
                var attrPath = $"{elementPath}/@{actAttr.Name.LocalName}";
                return new DiffResult(attrPath, "(missing)", Quote(actAttr.Value));
            }
        }

        return new DiffResult(elementPath, "", "", HasDifference: false);
    }

    private static string FormatNode(XNode? node)
    {
        return node switch
        {
            null => "null",
            XDocument doc => doc.Root is not null ? $"<{doc.Root.Name.LocalName}>..." : "(empty document)",
            XElement elem => FormatElement(elem),
            _ => node.ToString()
        };
    }

    private static string FormatElement(XElement? element)
    {
        if (element is null)
        {
            return "null";
        }

        if (element.HasElements)
        {
            return $"<{element.Name.LocalName}>...</{element.Name.LocalName}>";
        }

        return $"<{element.Name.LocalName}>{element.Value}</{element.Name.LocalName}>";
    }

    private static string Quote(string value)
    {
        if (value.Length > 50)
        {
            return $"\"{value[..47]}...\"";
        }

        return $"\"{value}\"";
    }
}
