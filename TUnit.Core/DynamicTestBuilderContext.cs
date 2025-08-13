namespace TUnit.Core;

/// <summary>
/// Context for building dynamic tests
/// </summary>
public class DynamicTestBuilderContext
{
    private readonly List<DynamicTest> _tests =
    [
    ];

    public DynamicTestBuilderContext(string filePath, int lineNumber)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
    }

    public string FilePath { get; }
    public int LineNumber { get; }

    public IReadOnlyList<DynamicTest> Tests => _tests.AsReadOnly();

    public void AddTest(DynamicTest test)
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
