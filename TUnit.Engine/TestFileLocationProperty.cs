using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Test file location property
/// </summary>
public class TestFileLocationProperty : IProperty
{
    public string FilePath { get; }
    public LinePositionSpan LineSpan { get; }
    
    public TestFileLocationProperty(string filePath, LinePositionSpan lineSpan)
    {
        FilePath = filePath;
        LineSpan = lineSpan;
    }
}