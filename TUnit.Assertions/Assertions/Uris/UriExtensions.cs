// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace TUnit.Assertions.Assertions.Uris;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsAbsoluteUri))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsBaseOf))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsDefaultPort))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsFile))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsLoopback))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsUnc))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsWellFormedOriginalString))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsWellFormedUriString))]
public static partial class UriIsExtensions {
    
}