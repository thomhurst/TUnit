using System.Text;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Encoding type checks
[CreateAssertion(typeof(Encoding), typeof(EncodingAssertionExtensions), nameof(IsUTF8))]
[CreateAssertion(typeof(Encoding), typeof(EncodingAssertionExtensions), nameof(IsUTF8), CustomName = "IsNotUTF8", NegateLogic = true)]

[CreateAssertion(typeof(Encoding), typeof(EncodingAssertionExtensions), nameof(IsASCII))]
[CreateAssertion(typeof(Encoding), typeof(EncodingAssertionExtensions), nameof(IsASCII), CustomName = "IsNotASCII", NegateLogic = true)]

[CreateAssertion(typeof(Encoding), typeof(EncodingAssertionExtensions), nameof(IsUnicode))]
[CreateAssertion(typeof(Encoding), typeof(EncodingAssertionExtensions), nameof(IsUnicode), CustomName = "IsNotUnicode", NegateLogic = true)]

[CreateAssertion(typeof(Encoding), typeof(EncodingAssertionExtensions), nameof(IsUTF32))]
[CreateAssertion(typeof(Encoding), typeof(EncodingAssertionExtensions), nameof(IsUTF32), CustomName = "IsNotUTF32", NegateLogic = true)]

[CreateAssertion(typeof(Encoding), typeof(EncodingAssertionExtensions), nameof(IsBigEndianUnicode))]
[CreateAssertion(typeof(Encoding), typeof(EncodingAssertionExtensions), nameof(IsBigEndianUnicode), CustomName = "IsNotBigEndianUnicode", NegateLogic = true)]

[CreateAssertion(typeof(Encoding), nameof(Encoding.IsSingleByte))]
[CreateAssertion(typeof(Encoding), nameof(Encoding.IsSingleByte), CustomName = "IsNotSingleByte", NegateLogic = true)]
public static partial class EncodingAssertionExtensions
{
    internal static bool IsUTF8(Encoding encoding) => encoding.Equals(Encoding.UTF8);
    internal static bool IsASCII(Encoding encoding) => encoding.Equals(Encoding.ASCII);
    internal static bool IsUnicode(Encoding encoding) => encoding.Equals(Encoding.Unicode);
    internal static bool IsUTF32(Encoding encoding) => encoding.Equals(Encoding.UTF32);
    internal static bool IsBigEndianUnicode(Encoding encoding) => encoding.Equals(Encoding.BigEndianUnicode);
}