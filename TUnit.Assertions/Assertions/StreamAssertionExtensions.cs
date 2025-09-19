using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<Stream>( nameof(Stream.CanRead))]
[CreateAssertion<Stream>( nameof(Stream.CanRead), CustomName = "CannotRead", NegateLogic = true)]

[CreateAssertion<Stream>( nameof(Stream.CanWrite))]
[CreateAssertion<Stream>( nameof(Stream.CanWrite), CustomName = "CannotWrite", NegateLogic = true)]

[CreateAssertion<Stream>( nameof(Stream.CanSeek))]
[CreateAssertion<Stream>( nameof(Stream.CanSeek), CustomName = "CannotSeek", NegateLogic = true)]

#if NET5_0_OR_GREATER
[CreateAssertion<Stream>( nameof(Stream.CanTimeout))]
[CreateAssertion<Stream>( nameof(Stream.CanTimeout), CustomName = "CannotTimeout", NegateLogic = true)]
#endif

// Custom helper methods
[CreateAssertion<Stream>( typeof(StreamAssertionExtensions), nameof(IsAtStart))]
[CreateAssertion<Stream>( typeof(StreamAssertionExtensions), nameof(IsAtStart), CustomName = "IsNotAtStart", NegateLogic = true)]

[CreateAssertion<Stream>( typeof(StreamAssertionExtensions), nameof(IsAtEnd))]
[CreateAssertion<Stream>( typeof(StreamAssertionExtensions), nameof(IsAtEnd), CustomName = "IsNotAtEnd", NegateLogic = true)]

[CreateAssertion<Stream>( typeof(StreamAssertionExtensions), nameof(IsEmpty))]
[CreateAssertion<Stream>( typeof(StreamAssertionExtensions), nameof(IsEmpty), CustomName = "IsNotEmpty", NegateLogic = true)]
public static partial class StreamAssertionExtensions
{
    internal static bool IsAtStart(Stream stream) => stream.CanSeek && stream.Position == 0;
    internal static bool IsAtEnd(Stream stream) => stream.CanSeek && stream.Position == stream.Length;
    internal static bool IsEmpty(Stream stream) => stream.CanSeek && stream.Length == 0;
}