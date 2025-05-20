namespace TUnit.Assertions.Assertions.Uris;

[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsAbsoluteUri))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsBaseOf))] // TODO requires extra options
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsDefaultPort))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsFile))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsLoopback))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsUnc))]
[GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsWellFormedOriginalString))]
// [GenerateAssertion<Uri>(AssertionType.Is, nameof(Uri.IsWellFormedUriString))] //  TODO requires extra options
public static partial class UriIsExtensions {
    
}