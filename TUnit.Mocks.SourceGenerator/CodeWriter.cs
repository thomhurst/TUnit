using System;
using System.Text;

namespace TUnit.Mocks.SourceGenerator;

/// <summary>
/// StringBuilder wrapper with indentation management for generating C# source code.
/// </summary>
internal sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;
    private const string IndentString = "    ";

    public void AppendLine(string line = "")
    {
        if (string.IsNullOrEmpty(line))
        {
            _sb.AppendLine();
            return;
        }

        for (int i = 0; i < _indent; i++)
            _sb.Append(IndentString);
        _sb.AppendLine(line);
    }

    public void Append(string text)
    {
        _sb.Append(text);
    }

    public void OpenBrace()
    {
        AppendLine("{");
        _indent++;
    }

    public void CloseBrace(string suffix = "")
    {
        _indent--;
        AppendLine("}" + suffix);
    }

    public IDisposable Block(string? header = null)
    {
        if (header != null) AppendLine(header);
        OpenBrace();
        return new BlockScope(this);
    }

    public void IncreaseIndent() => _indent++;
    public void DecreaseIndent() => _indent--;

    public override string ToString() => _sb.ToString();

    private sealed class BlockScope : IDisposable
    {
        private readonly CodeWriter _writer;
        public BlockScope(CodeWriter writer) => _writer = writer;
        public void Dispose() => _writer.CloseBrace();
    }
}
