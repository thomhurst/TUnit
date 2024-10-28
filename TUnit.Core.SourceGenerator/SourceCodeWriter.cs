using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TUnit.Core.SourceGenerator;

public class SourceCodeWriter : IDisposable
{
    private int _tabLevel;
    private readonly StringBuilder _stringBuilder = new();

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
    
    public void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }
        
        if (value[0] is '}' or ']')
        {
            _tabLevel--;
        }
        
        WriteTabs();
        
        _stringBuilder.AppendLine(value);

        if (value[0] is '{' or '[')
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