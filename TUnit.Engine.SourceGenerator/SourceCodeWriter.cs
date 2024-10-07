using System.Text;

namespace TUnit.Engine.SourceGenerator;

internal class SourceCodeWriter : IDisposable
{
    private int _tabLevel;
    private readonly StringBuilder _stringBuilder = new();
    private bool _lastWriteContainedNewLine;

    public void WriteLine()
    {
        _stringBuilder.AppendLine();
    }

    public void WriteTabs()
    {
        for (var i = 0; i < _tabLevel; i++)
        {
            _stringBuilder.Append('\t');
        }
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

        if (_lastWriteContainedNewLine)
        {
            WriteTabs();
        }

        _lastWriteContainedNewLine = true;

        _stringBuilder.AppendLine(value);

        if (value[0] == '{')
        {
            _tabLevel++;
        }
    }

    public void Write(string value)
    {
        _stringBuilder.Append(value);
        _lastWriteContainedNewLine = false;
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