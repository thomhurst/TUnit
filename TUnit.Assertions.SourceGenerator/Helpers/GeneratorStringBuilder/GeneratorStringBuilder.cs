using System.Collections.Immutable;
using System.Text;

namespace TUnit.Assertions.SourceGenerator.Helpers;

/// <summary>
///     A utility class for building text-based generators, providing methods to construct consistent
///     and formatted output such as namespace declarations, using statements, and indented text bodies.
/// </summary>
public class GeneratorStringBuilder(int paddingChars = 4) {
    /// <summary>
    ///     An instance of <see cref="StringBuilder" /> used internally to construct the string content
    ///     within the <see cref="GeneratorStringBuilder" /> class.
    /// </summary>
    internal readonly StringBuilder Builder = new();

    /// <summary>
    ///     Represents the number of characters used for indentation or padding in the generated output.
    ///     This value must be a positive integer, representing spaces per level of indentation. Defaults to 4.
    /// </summary>
    private readonly int _paddingChars = paddingChars > 0 ? paddingChars : 4;

    /// <summary>
    ///     Stores the current level of indentation for the <see cref="GeneratorStringBuilder" />.
    ///     This value determines the number of indentation levels applied when appending formatted outputs.
    /// </summary>
    private int _indent;

    /// <summary>
    ///     Gets or sets the number of indentation levels applied in the generated output.
    /// </summary>
    internal int IndentAmount {
        get => _indent;
        private set => _indent = value <= 0 ? 0 : value;
    }

    /// <summary>
    /// A cached collection of precomputed indentation strings. Utilized to optimize the generation
    /// of indented text by reducing runtime string allocations.
    /// </summary>
    private static ImmutableDictionary<(int, int), string> _indentStrings = new Dictionary<(int, int), string> {
        [(0, 4)] = new(' ', 0 * 4),
        [(1, 4)] = new(' ', 1 * 4),
        [(2, 4)] = new(' ', 2 * 4),
        [(3, 4)] = new(' ', 3 * 4), // Most common is up to 4 indents with 4 characters
    }.ToImmutableDictionary();


    #region Append (straight stringbuilder)

    /// <summary>
    ///     Appends a single character to the current string being built by the <see cref="GeneratorStringBuilder" />.
    /// </summary>
    /// <param name="c">The character to append to the string.</param>
    /// <returns>The current instance of <see cref="GeneratorStringBuilder" />, allowing for method chaining.</returns>
    public GeneratorStringBuilder Append(char c) {
        Builder.Append(c);
        return this;
    }

    /// <summary>
    ///     Appends the specified text to the current string builder instance.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <returns>The current instance of <see cref="GeneratorStringBuilder" />, allowing for method chaining.</returns>
    public GeneratorStringBuilder Append(string text) {
        Builder.Append(text);
        return this;
    }

    #endregion

    #region AppendLine methods(stringbuilder + indent)

    /// <summary>
    ///     Appends a line terminator to the end of the current string content.
    /// </summary>
    /// <returns>The current instance of <see cref="GeneratorStringBuilder" />, allowing for method chaining.</returns>
    public GeneratorStringBuilder AppendLine() {
        Builder.AppendLine();
        return this;
    }

    /// <summary>
    ///     Appends the specified text to the StringBuilder instance, followed by a newline character,
    ///     and applies the current indentation level.
    /// </summary>
    /// <param name="text">The text to be appended.</param>
    /// <returns>The current instance of <see cref="GeneratorStringBuilder" /> with the appended text.</returns>
    public GeneratorStringBuilder AppendLine(string text) {
        Builder
            .Append(IndentString(IndentAmount))
            .AppendLine(text);
        return this;
    }

    #endregion

    #region Auto Indented methods

    /// <summary>
    ///     Generates an indentation string based on the specified indentation level and predefined padding character count.
    /// </summary>
    /// <param name="amount">The level of indentation, typically representing the number of indentation units.</param>
    /// <returns>A string containing the appropriate number of spaces for the specified indentation level.</returns>
    internal string IndentString(int amount) {
        if (_indentStrings.TryGetValue((amount, _paddingChars), out string? indentString)) return indentString;

        indentString = new string(' ', amount * _paddingChars);
        _indentStrings = _indentStrings.Add((amount, _paddingChars), indentString);
        return indentString;
    }

    /// <summary>
    ///     Indents the subsequent appending of text or actions by a specified level.
    /// </summary>
    /// <param name="indentedAction">
    ///     An action that specifies the content to be appended, which is indented to match the current indentation level.
    /// </param>
    /// <returns>The current instance of <see cref="GeneratorStringBuilder" />, allowing for method chaining.</returns>
    public GeneratorStringBuilder Indent(Action<GeneratorStringBuilder> indentedAction) {
        IndentAmount++;
        indentedAction(this);
        IndentAmount--;
        return this;
    }

    /// <summary>
    /// Increases the current indentation level, executes the specified action with the provided argument, and
    /// then restores the previous indentation level after the action is completed.
    /// </summary>
    /// <typeparam name="T">The type of the argument passed to the specified action.</typeparam>
    /// <param name="indentedAction">The action to execute with the increased indentation level and the provided argument.</param>
    /// <param name="arg">The argument to pass to the specified action.</param>
    /// <returns>The current instance of <see cref="GeneratorStringBuilder" />, allowing for method chaining.</returns>
    public GeneratorStringBuilder Indent<T>(Action<GeneratorStringBuilder, T> indentedAction, T arg) {
        IndentAmount++;
        indentedAction(this, arg);
        IndentAmount--;
        return this;
    }

    /// <summary>
    ///     Appends a line of text with the current indentation.
    ///     The method adds the given text to the builder, prepended with the calculated indentation spaces.
    /// </summary>
    /// <param name="text">The line of text to append, indented according to the current indentation level.</param>
    /// <returns>The current instance of <see cref="GeneratorStringBuilder" />, allowing for method chaining.</returns>
    public GeneratorStringBuilder AppendLineIndented(string text)
        => Indent(static (g, t) => g.AppendLine(t), text);

    /// <summary>
    ///     Appends the provided text to the body of the string builder with the current indentation applied.
    /// </summary>
    /// <param name="text">The text to be appended to the body.</param>
    /// <returns>The current instance of <see cref="GeneratorStringBuilder" />, allowing for method chaining.</returns>
    public GeneratorStringBuilder AppendBody(string text) {
        string indent = IndentString(IndentAmount); // Cache the indent string
        var start = 0;
        for (var i = 0; i < text.Length; i++) {
            if (text[i] != '\r' && text[i] != '\n') continue;

            Builder
                .Append(indent)
                .AppendLine(text.Substring(start, i - start));

            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n') {
                i++;
            }

            start = i + 1;
        }

        // Append the last line if text does not end with a newline
        if (start < text.Length) {
            Builder
                .Append(indent)
                .AppendLine(text.Substring(start));
        }

        return this;
    }

    /// <summary>
    ///     Appends the specified text to the builder, applying the current indentation level.
    /// </summary>
    /// <param name="text">The text to append, which will respect the current indentation level.</param>
    /// <returns>The current instance of <see cref="GeneratorStringBuilder" />, allowing for method chaining.</returns>
    public GeneratorStringBuilder AppendBodyIndented(string text)
        => Indent(static (g, text) => g.AppendBody(text), text);

    #endregion

    #region ToString & Clear

    /// <summary>
    /// Converts the content of the GeneratorStringBuilder instance to a string representation.
    /// </summary>
    /// <returns>
    ///     A string that represents the current content of the GeneratorStringBuilder instance.
    /// </returns>
    public override string ToString()
        => Builder.ToString();

    /// <summary>
    /// Returns the current string representation of the builder and clears its content.
    /// </summary>
    /// <return>The string representation of the builder before it was cleared.</return>
    public string ToStringAndClear() {
        string result = ToString();
        Clear();
        return result;
    }

    /// <summary>
    /// </summary>
    /// Clears the internal string builder and resets the indentation amount to zero.
    /// <return>An instance of the current GeneratorStringBuilder class with its state cleared.</return>
    public GeneratorStringBuilder Clear() {
        Builder.Clear();
        IndentAmount = 0;
        return this;
    }

    #endregion
}