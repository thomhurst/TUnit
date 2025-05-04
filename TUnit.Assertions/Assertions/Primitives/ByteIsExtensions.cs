// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------

namespace TUnit.Assertions.Extensions;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
#if NET
[GenerateAssertion<byte>(AssertionType.Is, nameof(byte.IsPow2))]
[GenerateAssertion<byte>(AssertionType.Is, nameof(byte.IsEvenInteger))]
[GenerateAssertion<byte>(AssertionType.Is, nameof(byte.IsOddInteger))]
#endif
public static partial class ByteIsExtensions {
    
}