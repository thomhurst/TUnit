namespace TUnit.Assertions.Assertions.Uris;


[GenerateAssertion<Uri>(AssertionType.IsNot, nameof(Uri.IsAbsoluteUri))]
// [GenerateAssertion<Uri>(AssertionType.IsNot, nameof(Uri.IsBaseOf))] // TODO requires extra options
[GenerateAssertion<Uri>(AssertionType.IsNot, nameof(Uri.IsDefaultPort))]
[GenerateAssertion<Uri>(AssertionType.IsNot, nameof(Uri.IsFile))]
[GenerateAssertion<Uri>(AssertionType.IsNot, nameof(Uri.IsLoopback))]
[GenerateAssertion<Uri>(AssertionType.IsNot, nameof(Uri.IsUnc))]
[GenerateAssertion<Uri>(AssertionType.IsNot, nameof(Uri.IsWellFormedOriginalString))]
// [GenerateAssertion<Uri>(AssertionType.IsNot, nameof(Uri.IsWellFormedUriString))] //  TODO requires extra options
public static partial class UriIsNotExtensions {
    
}