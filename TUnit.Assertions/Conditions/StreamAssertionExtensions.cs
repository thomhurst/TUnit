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
}
