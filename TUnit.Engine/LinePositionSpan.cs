namespace TUnit.Engine;

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