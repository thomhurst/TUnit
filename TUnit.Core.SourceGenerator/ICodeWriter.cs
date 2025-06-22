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
    /// Ensures that the next text appended will start on a new line.
    /// </summary>
    ICodeWriter EnsureNewLine();
    
    /// <summary>
    /// Increases the indentation level.
    /// </summary>
    ICodeWriter Indent();
    
    /// <summary>
    /// Decreases the indentation level.
    /// </summary>
    ICodeWriter Unindent();
    
    /// <summary>
    /// Begins a code block with automatic formatting, handling opening brace and indentation.
    /// Returns an IDisposable that will unindent and append closing brace when disposed.
    /// </summary>
    IDisposable BeginBlock(string leadingText = "");
    
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
    
    /// <summary>
    /// Sets the initial indentation level. Useful for inline code generation.
    /// </summary>
    ICodeWriter SetIndentLevel(int level);
    
    /// <summary>
    /// Begins an object initializer block that ensures balanced braces.
    /// Automatically handles indentation and comma/semicolon placement.
    /// </summary>
    /// <param name="declaration">The declaration (e.g., "var x = new Foo" or "Property = new Bar")</param>
    /// <param name="terminator">The terminator after closing brace (e.g., ";", ",", or "")</param>
    IDisposable BeginObjectInitializer(string declaration, string terminator = ";");
    
    /// <summary>
    /// Begins an array initializer block that ensures balanced braces.
    /// Automatically handles indentation and proper formatting for array elements.
    /// </summary>
    /// <param name="declaration">The array declaration (e.g., "new int[]" or "new string[]")</param>
    /// <param name="terminator">The terminator after closing brace (e.g., ";", ",", or "")</param>
    /// <param name="inline">Whether to format the array inline (single line) or multi-line</param>
    IDisposable BeginArrayInitializer(string declaration, string terminator = "", bool inline = false);
}