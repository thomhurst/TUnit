using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Common test node properties
/// </summary>
public static class TestNodeProperties
{
    public static readonly string FullyQualifiedName = "FullyQualifiedName";
    public static readonly string DisplayName = "DisplayName";
    public static readonly string TestFileLocation = "TestFileLocation";
    public static readonly string Traits = "Traits";
}

/// <summary>
/// Passed test state property
/// </summary>
public class PassedTestNodeStateProperty : IProperty
{
    public static readonly PassedTestNodeStateProperty CachedInstance = new();
}

/// <summary>
/// Failed test state property
/// </summary>
public class FailedTestNodeStateProperty : IProperty
{
    public Exception Exception { get; }
    
    public FailedTestNodeStateProperty(Exception exception)
    {
        Exception = exception;
    }
}

/// <summary>
/// Skipped test state property
/// </summary>
public class SkippedTestNodeStateProperty : IProperty
{
    public string Reason { get; }
    
    public SkippedTestNodeStateProperty(string reason)
    {
        Reason = reason;
    }
}

/// <summary>
/// Timeout test state property
/// </summary>
public class TimeoutTestNodeStateProperty : IProperty
{
    public string Message { get; }
    
    public TimeoutTestNodeStateProperty(string message)
    {
        Message = message;
    }
}

/// <summary>
/// Cancelled test state property
/// </summary>
public class CancelledTestNodeStateProperty : IProperty
{
}

/// <summary>
/// Discovered test state property
/// </summary>
public class DiscoveredTestNodeStateProperty : IProperty
{
    public static readonly DiscoveredTestNodeStateProperty CachedInstance = new();
}

/// <summary>
/// In-progress test state property
/// </summary>
public class InProgressTestNodeStateProperty : IProperty
{
    public static readonly InProgressTestNodeStateProperty CachedInstance = new();
}

/// <summary>
/// Standard output property
/// </summary>
public class StandardOutputProperty : IProperty
{
    public string Output { get; }
    
    public StandardOutputProperty(string output)
    {
        Output = output;
    }
}

/// <summary>
/// Standard error property
/// </summary>
public class StandardErrorProperty : IProperty
{
    public string Error { get; }
    
    public StandardErrorProperty(string error)
    {
        Error = error;
    }
}

/// <summary>
/// Test metadata property
/// </summary>
public class TestMetadataProperty : IProperty
{
    public string Value { get; }
    
    public TestMetadataProperty(string value)
    {
        Value = value;
    }
}

/// <summary>
/// Duration property for test execution time
/// </summary>
public class DurationProperty : IProperty
{
    public TimeSpan Duration { get; }
    
    public DurationProperty(TimeSpan duration)
    {
        Duration = duration;
    }
}

/// <summary>
/// Timing property
/// </summary>
public class TimingProperty : IProperty
{
    public TimeSpan Duration { get; }
    
    public TimingProperty(TimeSpan duration)
    {
        Duration = duration;
    }
}

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

/// <summary>
/// Line position
/// </summary>
public struct LinePosition
{
    public int Line { get; }
    public int Column { get; }
    
    public LinePosition(int line, int column)
    {
        Line = line;
        Column = column;
    }
}

/// <summary>
/// Line position span
/// </summary>
public struct LinePositionSpan
{
    public LinePosition Start { get; }
    public LinePosition End { get; }
    
    public LinePositionSpan(LinePosition start, LinePosition end)
    {
        Start = start;
        End = end;
    }
}

/// <summary>
/// Trait property
/// </summary>
public class Trait
{
    public string Name { get; }
    public string Value { get; }
    
    public Trait(string name, string value)
    {
        Name = name;
        Value = value;
    }
}

/// <summary>
/// Key-value pair string property
/// </summary>
public class KeyValuePairStringProperty : IProperty
{
    public string Key { get; }
    public string Value { get; }
    
    public KeyValuePairStringProperty(string key, string value)
    {
        Key = key;
        Value = value;
    }
}