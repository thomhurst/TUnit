using System.Text;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Encoding type checks
[CreateAssertion<Encoding>( typeof(EncodingAssertionExtensions), nameof(IsUTF8))]
[CreateAssertion<Encoding>( typeof(EncodingAssertionExtensions), nameof(IsUTF8), CustomName = "IsNotUTF8", NegateLogic = true)]

[CreateAssertion<Encoding>( typeof(EncodingAssertionExtensions), nameof(IsASCII))]
[CreateAssertion<Encoding>( typeof(EncodingAssertionExtensions), nameof(IsASCII), CustomName = "IsNotASCII", NegateLogic = true)]

[CreateAssertion<Encoding>( typeof(EncodingAssertionExtensions), nameof(IsUnicode))]
[CreateAssertion<Encoding>( typeof(EncodingAssertionExtensions), nameof(IsUnicode), CustomName = "IsNotUnicode", NegateLogic = true)]

[CreateAssertion<Encoding>( typeof(EncodingAssertionExtensions), nameof(IsUTF32))]
[CreateAssertion<Encoding>( typeof(EncodingAssertionExtensions), nameof(IsUTF32), CustomName = "IsNotUTF32", NegateLogic = true)]

[CreateAssertion<Encoding>( typeof(EncodingAssertionExtensions), nameof(IsBigEndianUnicode))]
[CreateAssertion<Encoding>( typeof(EncodingAssertionExtensions), nameof(IsBigEndianUnicode), CustomName = "IsNotBigEndianUnicode", NegateLogic = true)]

[CreateAssertion<Encoding>( nameof(Encoding.IsSingleByte))]
[CreateAssertion<Encoding>( nameof(Encoding.IsSingleByte), CustomName = "IsNotSingleByte", NegateLogic = true)]
public static partial class EncodingAssertionExtensions
{
    internal static bool IsUTF8(Encoding encoding) => encoding.Equals(Encoding.UTF8);
    internal static bool IsASCII(Encoding encoding) => encoding.Equals(Encoding.ASCII);
    internal static bool IsUnicode(Encoding encoding) => encoding.Equals(Encoding.Unicode);
    internal static bool IsUTF32(Encoding encoding) => encoding.Equals(Encoding.UTF32);
    internal static bool IsBigEndianUnicode(Encoding encoding) => encoding.Equals(Encoding.BigEndianUnicode);
}