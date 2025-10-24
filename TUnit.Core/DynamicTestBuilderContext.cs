using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Context for building dynamic tests
/// </summary>
public class DynamicTestBuilderContext
{
    private readonly List<AbstractDynamicTest> _tests =
    [
    ];

    public DynamicTestBuilderContext(string filePath, int lineNumber)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    public string FilePath { get; }
    public int LineNumber { get; }

    public IReadOnlyList<AbstractDynamicTest> Tests => _tests.AsReadOnly();

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Adding dynamic tests requires reflection which is not supported in native AOT scenarios.")]
    #endif
    public void AddTest(AbstractDynamicTest test)
    {
        // Set creator location if the test implements IDynamicTestCreatorLocation
        if (test is IDynamicTestCreatorLocation testWithLocation)
        {
            testWithLocation.CreatorFilePath = FilePath;
            testWithLocation.CreatorLineNumber = LineNumber;
        }

        _tests.Add(test);
    }
}
