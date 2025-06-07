using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TUnit.Core.SourceGenerator;

public class SourceCodeWriter(int tabLevel = 0, char lastCharacter = ' ') : IDisposable
{
    private int _tabLevel = tabLevel;
    private readonly StringBuilder _stringBuilder = new();

    private static char[] _startOfStringTabLevelIncreasingChars = ['{', '['];
    private static char[] _startOfStringTabLevelDecreasingChars = ['}', ']'];

    private static char[] _endOfStringNewLineTriggerringChars = [',', ';'];


    public void WriteLine()
    {
        _stringBuilder.AppendLine();
    }

    public void Write([StringSyntax("c#")] string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var tempTabCount = 0;
        if (value.Length > 0 && value[0] == '\t')
        {
            tempTabCount = value.TakeWhile(c => c == '\t').Count();
        }

        _tabLevel -= tempTabCount;

        if (_startOfStringTabLevelDecreasingChars.Contains(value[0]))
        {
            _tabLevel--;
        }

        for (var i = 0; i < _tabLevel; i++)
        {
            _stringBuilder.Append('\t');
        }

        _stringBuilder.Append(value);

        if (_endOfStringNewLineTriggerringChars.Contains(value[^1]))
        {
            _stringBuilder.AppendLine();
        }

        if (_startOfStringTabLevelIncreasingChars.Contains(value[0]))
        {
            _tabLevel++;
        }

        _tabLevel += tempTabCount;
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
