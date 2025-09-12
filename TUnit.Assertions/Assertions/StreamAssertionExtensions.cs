using System.IO;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion(typeof(Stream), nameof(Stream.CanRead))]
[CreateAssertion(typeof(Stream), nameof(Stream.CanRead), CustomName = "CannotRead", NegateLogic = true)]

[CreateAssertion(typeof(Stream), nameof(Stream.CanWrite))]
[CreateAssertion(typeof(Stream), nameof(Stream.CanWrite), CustomName = "CannotWrite", NegateLogic = true)]

[CreateAssertion(typeof(Stream), nameof(Stream.CanSeek))]
[CreateAssertion(typeof(Stream), nameof(Stream.CanSeek), CustomName = "CannotSeek", NegateLogic = true)]

#if NET5_0_OR_GREATER
[CreateAssertion(typeof(Stream), nameof(Stream.CanTimeout))]
[CreateAssertion(typeof(Stream), nameof(Stream.CanTimeout), CustomName = "CannotTimeout", NegateLogic = true)]
#endif

// Custom helper methods
[CreateAssertion(typeof(Stream), typeof(StreamAssertionExtensions), nameof(IsAtStart))]
[CreateAssertion(typeof(Stream), typeof(StreamAssertionExtensions), nameof(IsAtStart), CustomName = "IsNotAtStart", NegateLogic = true)]

[CreateAssertion(typeof(Stream), typeof(StreamAssertionExtensions), nameof(IsAtEnd))]
[CreateAssertion(typeof(Stream), typeof(StreamAssertionExtensions), nameof(IsAtEnd), CustomName = "IsNotAtEnd", NegateLogic = true)]

[CreateAssertion(typeof(Stream), typeof(StreamAssertionExtensions), nameof(IsEmpty))]
[CreateAssertion(typeof(Stream), typeof(StreamAssertionExtensions), nameof(IsEmpty), CustomName = "IsNotEmpty", NegateLogic = true)]
public static partial class StreamAssertionExtensions
{
    internal static bool IsAtStart(Stream stream) => stream.CanSeek && stream.Position == 0;
    internal static bool IsAtEnd(Stream stream) => stream.CanSeek && stream.Position == stream.Length;
    internal static bool IsEmpty(Stream stream) => stream.CanSeek && stream.Length == 0;
}