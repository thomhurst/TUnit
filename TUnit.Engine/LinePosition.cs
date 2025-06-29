namespace TUnit.Engine;

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