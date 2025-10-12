using System.Text;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Encoding type using [GenerateAssertion] attributes.
/// These wrap encoding equality checks as extension methods.
/// </summary>
public static class EncodingAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be UTF-8 encoding")]
    public static bool IsUTF8(this Encoding value) => value?.Equals(Encoding.UTF8) == true;

    [GenerateAssertion(ExpectationMessage = "to not be UTF-8 encoding")]
    public static bool IsNotUTF8(this Encoding value) => !(value?.Equals(Encoding.UTF8) == true);

    [GenerateAssertion(ExpectationMessage = "to be ASCII encoding")]
    public static bool IsASCII(this Encoding value) => value?.Equals(Encoding.ASCII) == true;

    [GenerateAssertion(ExpectationMessage = "to be Unicode encoding")]
    public static bool IsUnicode(this Encoding value) => value?.Equals(Encoding.Unicode) == true;

    [GenerateAssertion(ExpectationMessage = "to be UTF-32 encoding")]
    public static bool IsUTF32(this Encoding value) => value?.Equals(Encoding.UTF32) == true;

    [GenerateAssertion(ExpectationMessage = "to be big-endian Unicode encoding")]
    public static bool IsBigEndianUnicode(this Encoding value) => value?.Equals(Encoding.BigEndianUnicode) == true;
}
