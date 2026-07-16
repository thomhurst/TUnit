using System.Text;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Encoding type using [GenerateAssertion(InlineMethodBody = true)] and [AssertionFrom&lt;Encoding&gt;] attributes.
/// These wrap encoding equality checks and properties as extension methods.
/// </summary>
[AssertionFrom<Encoding>(nameof(Encoding.IsSingleByte), ExpectationMessage = "be single-byte encoding")]
[AssertionFrom<Encoding>(nameof(Encoding.IsSingleByte), CustomName = "IsNotSingleByte", NegateLogic = true, ExpectationMessage = "be single-byte encoding")]
file static partial class EncodingAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be UTF-8 encoding", InlineMethodBody = true)]
    public static bool IsUTF8(this Encoding value) => value?.Equals(Encoding.UTF8) == true;
    [GenerateAssertion(ExpectationMessage = "to not be UTF-8 encoding", InlineMethodBody = true)]
    public static bool IsNotUTF8(this Encoding value) => !(value?.Equals(Encoding.UTF8) == true);
    [GenerateAssertion(ExpectationMessage = "to be ASCII encoding", InlineMethodBody = true)]
    public static bool IsASCII(this Encoding value) => value?.Equals(Encoding.ASCII) == true;
    [GenerateAssertion(ExpectationMessage = "to be Unicode encoding", InlineMethodBody = true)]
    public static bool IsUnicode(this Encoding value) => value?.Equals(Encoding.Unicode) == true;
    [GenerateAssertion(ExpectationMessage = "to be UTF-32 encoding", InlineMethodBody = true)]
    public static bool IsUTF32(this Encoding value) => value?.Equals(Encoding.UTF32) == true;
    [GenerateAssertion(ExpectationMessage = "to be big-endian Unicode encoding", InlineMethodBody = true)]
    public static bool IsBigEndianUnicode(this Encoding value) => value?.Equals(Encoding.BigEndianUnicode) == true;
}
