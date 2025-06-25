namespace TUnit.Core;

/// <summary>
/// Context for building dynamic tests
/// </summary>
public class DynamicTestBuilderContext
{
    private readonly List<DynamicTest> _tests = new();
    
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
        _tests.Add(test);
    }
}