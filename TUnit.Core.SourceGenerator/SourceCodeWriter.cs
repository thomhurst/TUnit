using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Legacy source code writer that delegates to the new CodeWriter implementation.
/// This class is maintained for backward compatibility.
/// </summary>
[Obsolete("Use ICodeWriter and CodeWriter instead. This class will be removed in a future version.")]
public class SourceCodeWriter : ICodeWriter
{
    private readonly CodeWriter _writer;
    
    public int IndentLevel => _writer._indentLevel;
    
    // Legacy property for backward compatibility
    public int TabLevel
    {
        get => _writer._indentLevel;
        private set { /* No-op for compatibility */ }
    }

    public SourceCodeWriter(int initialIndentLevel = 0, string indentString = "\t")
    {
        _writer = new CodeWriter(indentString, includeHeader: initialIndentLevel == 0);
        
        // Apply initial indentation
        for (var i = 0; i < initialIndentLevel; i++)
        {
            _writer._indentLevel++;
        }
    }

    public void Write([StringSyntax("c#")] string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        _writer.Append(content.Trim());
    }

    public void WriteLine()
    {
        _writer.AppendLine();
    }

    public void WriteLine([StringSyntax("c#")] string content)
    {
        _writer.AppendLine(content.Trim());
    }

    public override string ToString()
    {
        return _writer.ToString();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
    
    // ICodeWriter implementation - delegate all calls to the internal writer
    public ICodeWriter AppendLine(string line = "") 
    {
        _writer.AppendLine(line);
        return this;
    }
    
    public ICodeWriter Append(string text)
    {
        _writer.Append(text);
        return this;
    }
    
    public ICodeWriter AppendLines(IEnumerable<string> lines)
    {
        _writer.AppendLines(lines);
        return this;
    }
    
    public ICodeWriter AppendLines(params string[] lines)
    {
        _writer.AppendLines(lines);
        return this;
    }
    
    public ICodeWriter AppendBlock(string header, Action<ICodeWriter> body)
    {
        _writer.AppendBlock(header, body);
        return this;
    }
    
    public IDisposable Block(string opener = "{", string closer = "}")
    {
        return _writer.Block(opener, closer);
    }
    
    public IDisposable Scope()
    {
        return _writer.Scope();
    }
    
    public ICodeWriter AppendLineIf(bool condition, string line)
    {
        _writer.AppendLineIf(condition, line);
        return this;
    }
    
    public ICodeWriter AppendComment(string comment)
    {
        _writer.AppendComment(comment);
        return this;
    }
    
    public ICodeWriter AppendRaw(string multilineText)
    {
        _writer.AppendRaw(multilineText);
        return this;
    }
}