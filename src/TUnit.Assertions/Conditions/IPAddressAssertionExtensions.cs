using System.Net;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for IPAddress type using [AssertionFrom&lt;IPAddress&gt;] attributes.
/// Each assertion wraps a property from the IPAddress class.
/// </summary>
[AssertionFrom<IPAddress>(nameof(IPAddress.IsIPv4MappedToIPv6), ExpectationMessage = "be an IPv4-mapped IPv6 address")]
[AssertionFrom<IPAddress>(nameof(IPAddress.IsIPv4MappedToIPv6), CustomName = "IsNotIPv4MappedToIPv6", NegateLogic = true, ExpectationMessage = "be an IPv4-mapped IPv6 address")]

[AssertionFrom<IPAddress>(nameof(IPAddress.IsIPv6LinkLocal), ExpectationMessage = "be an IPv6 link-local address")]
[AssertionFrom<IPAddress>(nameof(IPAddress.IsIPv6LinkLocal), CustomName = "IsNotIPv6LinkLocal", NegateLogic = true, ExpectationMessage = "be an IPv6 link-local address")]

[AssertionFrom<IPAddress>(nameof(IPAddress.IsIPv6Multicast), ExpectationMessage = "be an IPv6 multicast address")]
[AssertionFrom<IPAddress>(nameof(IPAddress.IsIPv6Multicast), CustomName = "IsNotIPv6Multicast", NegateLogic = true, ExpectationMessage = "be an IPv6 multicast address")]

[AssertionFrom<IPAddress>(nameof(IPAddress.IsIPv6SiteLocal), ExpectationMessage = "be an IPv6 site-local address")]
[AssertionFrom<IPAddress>(nameof(IPAddress.IsIPv6SiteLocal), CustomName = "IsNotIPv6SiteLocal", NegateLogic = true, ExpectationMessage = "be an IPv6 site-local address")]

[AssertionFrom<IPAddress>(nameof(IPAddress.IsIPv6Teredo), ExpectationMessage = "be an IPv6 Teredo address")]
[AssertionFrom<IPAddress>(nameof(IPAddress.IsIPv6Teredo), CustomName = "IsNotIPv6Teredo", NegateLogic = true, ExpectationMessage = "be an IPv6 Teredo address")]
public static partial class IPAddressAssertionExtensions
{
}
