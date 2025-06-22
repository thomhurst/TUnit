using System;
using System.Collections.Generic;

namespace TUnit.Core.SourceGenerator;

/// <summary>
/// Interface for fluent code generation with automatic formatting and indentation.
/// </summary>
public interface ICodeWriter : IDisposable
{
    /// <summary>
    /// Appends a line of code with proper indentation.
    /// </summary>
    ICodeWriter AppendLine(string line = "");
    
    /// <summary>
    /// Appends text without adding a new line.
    /// </summary>
    ICodeWriter Append(string text);
    
    /// <summary>
    /// Appends multiple lines of code.
    /// </summary>
    ICodeWriter AppendLines(IEnumerable<string> lines);
    
    /// <summary>
    /// Appends multiple lines of code.
    /// </summary>
    ICodeWriter AppendLines(params string[] lines);
    
    /// <summary>
    /// Appends a code block with automatic braces and indentation.
    /// </summary>
    ICodeWriter AppendBlock(string header, Action<ICodeWriter> body);
    
    /// <summary>
    /// Opens a block scope with automatic indentation. Use with using statement.
    /// </summary>
    IDisposable Block(string opener = "{", string closer = "}");
    
    /// <summary>
    /// Creates an indentation scope without braces. Use with using statement.
    /// </summary>
    IDisposable Scope();
    
    /// <summary>
    /// Conditionally appends a line.
    /// </summary>
    ICodeWriter AppendLineIf(bool condition, string line);
    
    /// <summary>
    /// Appends a single-line comment.
    /// </summary>
    ICodeWriter AppendComment(string comment);
    
    /// <summary>
    /// Appends raw multi-line text, handling indentation for each line.
    /// </summary>
    ICodeWriter AppendRaw(string multilineText);
    
    /// <summary>
    /// Gets the generated code as a string.
    /// </summary>
    string ToString();
}