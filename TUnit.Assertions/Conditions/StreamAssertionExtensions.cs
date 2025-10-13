using System.ComponentModel;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Stream type using [AssertionFrom&lt;Stream&gt;] attributes.
/// Each assertion wraps a property from the Stream class.
/// </summary>
[AssertionFrom<Stream>(nameof(Stream.CanRead), ExpectationMessage = "be readable")]
[AssertionFrom<Stream>(nameof(Stream.CanRead), CustomName = "CannotRead", NegateLogic = true, ExpectationMessage = "be readable")]

[AssertionFrom<Stream>(nameof(Stream.CanWrite), ExpectationMessage = "be writable")]
[AssertionFrom<Stream>(nameof(Stream.CanWrite), CustomName = "CannotWrite", NegateLogic = true, ExpectationMessage = "be writable")]

[AssertionFrom<Stream>(nameof(Stream.CanSeek), ExpectationMessage = "be seekable")]
[AssertionFrom<Stream>(nameof(Stream.CanSeek), CustomName = "CannotSeek", NegateLogic = true, ExpectationMessage = "be seekable")]

[AssertionFrom<Stream>(nameof(Stream.CanTimeout), ExpectationMessage = "support timeout")]
[AssertionFrom<Stream>(nameof(Stream.CanTimeout), CustomName = "CannotTimeout", NegateLogic = true, ExpectationMessage = "support timeout")]
public static partial class StreamAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be at the start")]
    public static bool IsAtStart(this Stream value) => value?.Position == 0;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be at the end")]
    public static bool IsAtEnd(this Stream value) => value != null && value.Position == value.Length;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be empty")]
    public static bool IsEmpty(this Stream value) => value?.Length == 0;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be empty")]
    public static bool IsNotEmpty(this Stream value) => value?.Length > 0;
}
