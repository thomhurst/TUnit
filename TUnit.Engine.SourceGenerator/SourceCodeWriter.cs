using System.Text;

namespace TUnit.Engine.SourceGenerator;

public class SourceCodeWriter : IDisposable
{
    private int _tabLevel;
    private readonly StringBuilder _stringBuilder = new();

    public void WriteLine()
    {
        _stringBuilder.AppendLine();
    }
    
    public void WriteLine(string value)
    {
        if (value.StartsWith("}"))
        {
            _tabLevel--;
        }

        for (var i = 0; i < _tabLevel; i++)
        {
            _stringBuilder.Append('\t');
        }
        
        _stringBuilder.AppendLine(value);

        if (value.StartsWith("{"))
        {
            _tabLevel++;
        }
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }

    public void Dispose()
    {
        _stringBuilder.Clear();
    }
}