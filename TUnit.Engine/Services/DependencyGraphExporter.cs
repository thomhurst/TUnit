using System.Text;
using TUnit.Core;

namespace TUnit.Engine.Services;

/// <summary>
/// Exports test dependency graphs in Mermaid diagram format.
/// </summary>
internal static class DependencyGraphExporter
{
    /// <summary>
    /// Generates a Mermaid diagram string representing the dependency graph of the given tests.
    /// </summary>
    /// <param name="tests">The collection of executable tests to visualize.</param>
    /// <returns>A string containing the Mermaid diagram, or null if there are no dependencies to show.</returns>
    public static string? GenerateMermaidDiagram(IReadOnlyList<AbstractExecutableTest> tests)
    {
        if (tests.Count == 0)
        {
            return null;
        }

        // Build a lookup from test to a stable short ID for the Mermaid node names
        var nodeIds = new Dictionary<string, string>(tests.Count, StringComparer.Ordinal);
        var nodeLabels = new Dictionary<string, string>(tests.Count, StringComparer.Ordinal);
        var testStates = new Dictionary<string, TestState>(tests.Count, StringComparer.Ordinal);

        for (var i = 0; i < tests.Count; i++)
        {
            var test = tests[i];
            var testId = test.TestId;
            var nodeId = $"T{i}";
            nodeIds[testId] = nodeId;

            var className = test.Metadata.TestClassType.Name;
            var methodName = test.Metadata.TestMethodName;
            nodeLabels[testId] = $"{className}.{methodName}";
            testStates[testId] = test.State;
        }

        // Collect all edges (dependency relationships)
        var edges = new List<(string FromNodeId, string ToNodeId, bool ProceedOnFailure)>();
        var testsWithDependencies = new HashSet<string>(StringComparer.Ordinal);

        foreach (var test in tests)
        {
            if (test.Dependencies.Length == 0)
            {
                continue;
            }

            var dependentNodeId = nodeIds[test.TestId];
            testsWithDependencies.Add(test.TestId);

            foreach (var dep in test.Dependencies)
            {
                var depTestId = dep.Test.TestId;
                testsWithDependencies.Add(depTestId);

                if (nodeIds.TryGetValue(depTestId, out var depNodeId))
                {
                    edges.Add((depNodeId, dependentNodeId, dep.ProceedOnFailure));
                }
            }
        }

        // If no dependencies exist at all, produce a minimal diagram noting that
        if (edges.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine("graph LR");

        // Emit node definitions for all tests that participate in dependencies
        foreach (var test in tests)
        {
            if (!testsWithDependencies.Contains(test.TestId))
            {
                continue;
            }

            var nodeId = nodeIds[test.TestId];
            var label = SanitizeMermaidLabel(nodeLabels[test.TestId]);
            sb.Append("    ").Append(nodeId).Append('[').Append('"').Append(label).Append('"').AppendLine("]");
        }

        sb.AppendLine();

        // Emit edges
        foreach (var (fromNodeId, toNodeId, proceedOnFailure) in edges)
        {
            if (proceedOnFailure)
            {
                sb.Append("    ").Append(fromNodeId).Append(" -.-> ").AppendLine(toNodeId);
            }
            else
            {
                sb.Append("    ").Append(fromNodeId).Append(" --> ").AppendLine(toNodeId);
            }
        }

        // Emit style classes for color coding based on test state
        var hasPassedTests = false;
        var hasFailedTests = false;
        var hasSkippedTests = false;
        var hasNotRunTests = false;

        var passedNodes = new List<string>();
        var failedNodes = new List<string>();
        var skippedNodes = new List<string>();
        var notRunNodes = new List<string>();

        foreach (var test in tests)
        {
            if (!testsWithDependencies.Contains(test.TestId))
            {
                continue;
            }

            var nodeId = nodeIds[test.TestId];
            var state = testStates[test.TestId];

            switch (state)
            {
                case TestState.Passed:
                    passedNodes.Add(nodeId);
                    hasPassedTests = true;
                    break;
                case TestState.Failed:
                case TestState.Timeout:
                    failedNodes.Add(nodeId);
                    hasFailedTests = true;
                    break;
                case TestState.Skipped:
                case TestState.Cancelled:
                    skippedNodes.Add(nodeId);
                    hasSkippedTests = true;
                    break;
                default:
                    notRunNodes.Add(nodeId);
                    hasNotRunTests = true;
                    break;
            }
        }

        sb.AppendLine();

        // Define style classes
        if (hasPassedTests)
        {
            sb.AppendLine("    classDef passed fill:#22c55e,stroke:#16a34a,color:#fff");
            sb.Append("    class ").Append(string.Join(",", passedNodes)).AppendLine(" passed");
        }

        if (hasFailedTests)
        {
            sb.AppendLine("    classDef failed fill:#ef4444,stroke:#dc2626,color:#fff");
            sb.Append("    class ").Append(string.Join(",", failedNodes)).AppendLine(" failed");
        }

        if (hasSkippedTests)
        {
            sb.AppendLine("    classDef skipped fill:#f59e0b,stroke:#d97706,color:#fff");
            sb.Append("    class ").Append(string.Join(",", skippedNodes)).AppendLine(" skipped");
        }

        if (hasNotRunTests)
        {
            sb.AppendLine("    classDef notrun fill:#94a3b8,stroke:#64748b,color:#fff");
            sb.Append("    class ").Append(string.Join(",", notRunNodes)).AppendLine(" notrun");
        }

        return sb.ToString();
    }

    private static string SanitizeMermaidLabel(string label)
    {
        // Escape characters that have special meaning in Mermaid
        return label
            .Replace("\"", "#quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
