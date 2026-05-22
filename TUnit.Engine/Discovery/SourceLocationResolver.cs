using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Discovery;

internal static class SourceLocationResolver
{
    private static readonly ConcurrentDictionary<string, string[]> SourceLinesCache = new(StringComparer.Ordinal);

    internal static SourceLocation Resolve(MethodInfo method)
    {
        var testAttribute = method.GetCustomAttributes().OfType<BaseTestAttribute>().FirstOrDefault();
        var filePath = testAttribute?.File ?? "Unknown";
        var lineNumber = testAttribute?.Line ?? 0;

        return new SourceLocation(
            filePath,
            lineNumber,
            StartColumnNumber: 0,
            EndLineNumber: TryInferSourceEndLine(filePath, lineNumber, method.Name) ?? lineNumber,
            EndColumnNumber: 0);
    }

    private static int? TryInferSourceEndLine(string filePath, int startLine, string methodName)
    {
        if (string.IsNullOrEmpty(filePath) || startLine <= 0 || string.IsNullOrEmpty(methodName))
        {
            return null;
        }

        if (!TryReadSourceLines(filePath, out var lines) || startLine > lines.Length)
        {
            return null;
        }

        var methodLineIndex = FindMethodDeclarationLine(lines, startLine - 1, methodName);
        if (methodLineIndex < 0)
        {
            return null;
        }

        var expressionBodyLine = FindExpressionBodyEndLine(lines, methodLineIndex);
        if (expressionBodyLine is not null)
        {
            return expressionBodyLine;
        }

        var bodyStart = FindFirstCharacter(lines, methodLineIndex, '{');
        return bodyStart is null ? null : FindMatchingBraceEndLine(lines, bodyStart.Value.LineIndex, bodyStart.Value.ColumnIndex);
    }

    private static bool TryReadSourceLines(string filePath, out string[] lines)
    {
        if (SourceLinesCache.TryGetValue(filePath, out lines!))
        {
            return true;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                lines = [];
                return false;
            }

            lines = File.ReadAllLines(filePath);
            SourceLinesCache.TryAdd(filePath, lines);
            return true;
        }
        catch
        {
            lines = [];
            return false;
        }
    }

    private static int FindMethodDeclarationLine(string[] lines, int startLineIndex, string methodName)
    {
        for (var i = startLineIndex; i < lines.Length; i++)
        {
            if (ContainsIdentifier(lines[i], methodName))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool ContainsIdentifier(string line, string identifier)
    {
        var index = line.IndexOf(identifier, StringComparison.Ordinal);
        while (index >= 0)
        {
            var before = index == 0 ? '\0' : line[index - 1];
            var afterIndex = index + identifier.Length;
            var after = afterIndex >= line.Length ? '\0' : line[afterIndex];
            if (!IsIdentifierChar(before) && !IsIdentifierChar(after))
            {
                return true;
            }

            index = line.IndexOf(identifier, index + identifier.Length, StringComparison.Ordinal);
        }

        return false;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static int? FindExpressionBodyEndLine(string[] lines, int methodLineIndex)
    {
        for (var i = methodLineIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            var bodyIndex = line.IndexOf('{');
            var expressionIndex = line.IndexOf("=>", StringComparison.Ordinal);
            if (bodyIndex >= 0 && (expressionIndex < 0 || bodyIndex < expressionIndex))
            {
                return null;
            }

            if (expressionIndex < 0)
            {
                continue;
            }

            for (var j = i; j < lines.Length; j++)
            {
                if (lines[j].IndexOf(';') >= 0)
                {
                    return j + 1;
                }
            }

            return null;
        }

        return null;
    }

    private static (int LineIndex, int ColumnIndex)? FindFirstCharacter(string[] lines, int startLineIndex, char character)
    {
        for (var i = startLineIndex; i < lines.Length; i++)
        {
            var index = lines[i].IndexOf(character);
            if (index >= 0)
            {
                return (i, index);
            }
        }

        return null;
    }

    private static int? FindMatchingBraceEndLine(string[] lines, int startLineIndex, int startColumnIndex)
    {
        var depth = 0;
        var inBlockComment = false;

        for (var i = startLineIndex; i < lines.Length; i++)
        {
            var line = lines[i];
            for (var j = i == startLineIndex ? startColumnIndex : 0; j < line.Length; j++)
            {
                if (inBlockComment)
                {
                    if (j + 1 < line.Length && line[j] == '*' && line[j + 1] == '/')
                    {
                        inBlockComment = false;
                        j++;
                    }

                    continue;
                }

                if (j + 1 < line.Length && line[j] == '/' && line[j + 1] == '/')
                {
                    break;
                }

                if (j + 1 < line.Length && line[j] == '/' && line[j + 1] == '*')
                {
                    inBlockComment = true;
                    j++;
                    continue;
                }

                if (line[j] is '"' or '\'')
                {
                    j = SkipQuotedLiteral(line, j);
                    continue;
                }

                if (line[j] == '{')
                {
                    depth++;
                }
                else if (line[j] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i + 1;
                    }
                }
            }
        }

        return null;
    }

    private static int SkipQuotedLiteral(string line, int startIndex)
    {
        var quote = line[startIndex];
        var isVerbatim = quote == '"' && startIndex > 0 && line[startIndex - 1] == '@';

        for (var i = startIndex + 1; i < line.Length; i++)
        {
            if (line[i] != quote)
            {
                continue;
            }

            if (isVerbatim && i + 1 < line.Length && line[i + 1] == '"')
            {
                i++;
                continue;
            }

            if (!isVerbatim && IsEscaped(line, i))
            {
                continue;
            }

            return i;
        }

        return line.Length - 1;
    }

    private static bool IsEscaped(string line, int index)
    {
        var slashCount = 0;
        for (var i = index - 1; i >= 0 && line[i] == '\\'; i--)
        {
            slashCount++;
        }

        return slashCount % 2 == 1;
    }
}

internal readonly record struct SourceLocation(
    string FilePath,
    int LineNumber,
    int StartColumnNumber,
    int EndLineNumber,
    int EndColumnNumber);
