using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

public static class EncodingAssertionExtensions
{
    public static IsUTF8EncodingAssertion IsUTF8(
        this IAssertionSource<Encoding> source)
    {
        source.ExpressionBuilder.Append(".IsUTF8()");
        return new IsUTF8EncodingAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsNotUTF8EncodingAssertion IsNotUTF8(
        this IAssertionSource<Encoding> source)
    {
        source.ExpressionBuilder.Append(".IsNotUTF8()");
        return new IsNotUTF8EncodingAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsASCIIEncodingAssertion IsASCII(
        this IAssertionSource<Encoding> source)
    {
        source.ExpressionBuilder.Append(".IsASCII()");
        return new IsASCIIEncodingAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsUnicodeEncodingAssertion IsUnicode(
        this IAssertionSource<Encoding> source)
    {
        source.ExpressionBuilder.Append(".IsUnicode()");
        return new IsUnicodeEncodingAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsUTF32EncodingAssertion IsUTF32(
        this IAssertionSource<Encoding> source)
    {
        source.ExpressionBuilder.Append(".IsUTF32()");
        return new IsUTF32EncodingAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsBigEndianUnicodeEncodingAssertion IsBigEndianUnicode(
        this IAssertionSource<Encoding> source)
    {
        source.ExpressionBuilder.Append(".IsBigEndianUnicode()");
        return new IsBigEndianUnicodeEncodingAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsSingleByteEncodingAssertion IsSingleByte(
        this IAssertionSource<Encoding> source)
    {
        source.ExpressionBuilder.Append(".IsSingleByte()");
        return new IsSingleByteEncodingAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsNotSingleByteEncodingAssertion IsNotSingleByte(
        this IAssertionSource<Encoding> source)
    {
        source.ExpressionBuilder.Append(".IsNotSingleByte()");
        return new IsNotSingleByteEncodingAssertion(source.Context, source.ExpressionBuilder);
    }
}
