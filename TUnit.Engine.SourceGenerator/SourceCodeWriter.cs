using System.Text;

namespace TUnit.Engine.SourceGenerator;

internal class SourceCodeWriter : IDisposable
{
    private int _tabLevel;
    private readonly StringBuilder _stringBuilder = new();

    public void WriteLine()
    {
        _stringBuilder.AppendLine();
    }
    
    public void WriteLine(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }
        
        if (value[0] == '}')
        {
            _tabLevel--;
        }

        for (var i = 0; i < _tabLevel; i++)
        {
            _stringBuilder.Append('\t');
        }
        
        _stringBuilder.AppendLine(value);

        if (value[0] == '{')
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