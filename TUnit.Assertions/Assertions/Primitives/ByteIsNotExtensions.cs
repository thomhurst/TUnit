// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace TUnit.Assertions.Extensions;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
#if NET
[GenerateAssertion<byte>(AssertionType.IsNot, nameof(byte.IsPow2))]
[GenerateAssertion<byte>(AssertionType.IsNot, nameof(byte.IsEvenInteger))]
[GenerateAssertion<byte>(AssertionType.IsNot, nameof(byte.IsOddInteger))]
#endif
public static partial class ByteIsNotExtensions {
    
}