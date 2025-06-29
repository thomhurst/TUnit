using System.Diagnostics;

namespace TUnit.Engine.Diagnostics;

/// <summary>
/// Custom trace listener that converts Debug.Assert/Trace.Assert failures into exceptions
/// </summary>
internal sealed class TUnitAssertionListener : TraceListener
{
    public override void Fail(string? message)
    {
        Fail(message, null);
    }
    
    public override void Fail(string? message, string? detailMessage)
    {
        var fullMessage = string.IsNullOrEmpty(detailMessage) 
            ? $"Assertion Failed: {message}" 
            : $"Assertion Failed: {message} - {detailMessage}";
        
        // Write to error stream first
        Console.Error.WriteLine(fullMessage);
        
        // Then throw an exception that can be caught by the test executor
        throw new TUnitAssertionFailedException(fullMessage);
    }
    
    public override void Write(string? message)
    {
        // Forward to console error stream
        Console.Error.Write(message);
    }
    
    public override void WriteLine(string? message)
    {
        // Forward to console error stream
        Console.Error.WriteLine(message);
    }
}