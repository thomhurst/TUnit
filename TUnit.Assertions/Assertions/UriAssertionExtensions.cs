using System;
using System;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

// Properties
[CreateAssertion<Uri>( nameof(Uri.IsAbsoluteUri))]
[CreateAssertion<Uri>( nameof(Uri.IsAbsoluteUri), CustomName = "IsNotAbsoluteUri", NegateLogic = true)]
[CreateAssertion<Uri>( nameof(Uri.IsDefaultPort))]
[CreateAssertion<Uri>( nameof(Uri.IsDefaultPort), CustomName = "IsNotDefaultPort", NegateLogic = true)]
[CreateAssertion<Uri>( nameof(Uri.IsFile))]
[CreateAssertion<Uri>( nameof(Uri.IsFile), CustomName = "IsNotFile", NegateLogic = true)]
[CreateAssertion<Uri>( nameof(Uri.IsLoopback))]
[CreateAssertion<Uri>( nameof(Uri.IsLoopback), CustomName = "IsNotLoopback", NegateLogic = true)]
[CreateAssertion<Uri>( nameof(Uri.IsUnc))]
[CreateAssertion<Uri>( nameof(Uri.IsUnc), CustomName = "IsNotUnc", NegateLogic = true)]

// Instance methods
[CreateAssertion<Uri>( nameof(Uri.IsBaseOf))]
[CreateAssertion<Uri>( nameof(Uri.IsBaseOf), CustomName = "IsNotBaseOf", NegateLogic = true)]
[CreateAssertion<Uri>( nameof(Uri.IsWellFormedOriginalString))]
[CreateAssertion<Uri>( nameof(Uri.IsWellFormedOriginalString), CustomName = "IsNotWellFormedOriginalString", NegateLogic = true)]

// Static methods
[CreateAssertion<string>( typeof(Uri), nameof(Uri.IsWellFormedUriString))]
[CreateAssertion<string>( typeof(Uri), nameof(Uri.IsWellFormedUriString), CustomName = "IsNotWellFormedUriString", NegateLogic = true)]
[CreateAssertion<char>( typeof(Uri), nameof(Uri.IsHexDigit))]
[CreateAssertion<char>( typeof(Uri), nameof(Uri.IsHexDigit), CustomName = "IsNotHexDigit", NegateLogic = true)]
[CreateAssertion<string>( typeof(Uri), nameof(Uri.IsHexEncoding))]
[CreateAssertion<string>( typeof(Uri), nameof(Uri.IsHexEncoding), CustomName = "IsNotHexEncoding", NegateLogic = true)]

[CreateAssertion<Uri>( typeof(UriAssertionExtensions), nameof(IsHttps))]
[CreateAssertion<Uri>( typeof(UriAssertionExtensions), nameof(IsHttps), CustomName = "IsNotHttps", NegateLogic = true)]

[CreateAssertion<Uri>( typeof(UriAssertionExtensions), nameof(IsHttp))]
[CreateAssertion<Uri>( typeof(UriAssertionExtensions), nameof(IsHttp), CustomName = "IsNotHttp", NegateLogic = true)]

[CreateAssertion<Uri>( typeof(UriAssertionExtensions), nameof(IsHttpOrHttps))]
[CreateAssertion<Uri>( typeof(UriAssertionExtensions), nameof(IsHttpOrHttps), CustomName = "IsNotHttpOrHttps", NegateLogic = true)]
public static partial class UriAssertionExtensions
{
    internal static bool IsHttps(Uri uri) => uri.Scheme == Uri.UriSchemeHttps;
    internal static bool IsHttp(Uri uri) => uri.Scheme == Uri.UriSchemeHttp;
    internal static bool IsHttpOrHttps(Uri uri) => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
}
