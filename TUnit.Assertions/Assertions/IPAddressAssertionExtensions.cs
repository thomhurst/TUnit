using System.Net;
using System.Net.Sockets;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

[CreateAssertion<IPAddress>( nameof(IPAddress.IsIPv4MappedToIPv6))]
[CreateAssertion<IPAddress>( nameof(IPAddress.IsIPv4MappedToIPv6), CustomName = "IsNotIPv4MappedToIPv6", NegateLogic = true)]

[CreateAssertion<IPAddress>( nameof(IPAddress.IsIPv6LinkLocal))]
[CreateAssertion<IPAddress>( nameof(IPAddress.IsIPv6LinkLocal), CustomName = "IsNotIPv6LinkLocal", NegateLogic = true)]

[CreateAssertion<IPAddress>( nameof(IPAddress.IsIPv6Multicast))]
[CreateAssertion<IPAddress>( nameof(IPAddress.IsIPv6Multicast), CustomName = "IsNotIPv6Multicast", NegateLogic = true)]

[CreateAssertion<IPAddress>( nameof(IPAddress.IsIPv6SiteLocal))]
[CreateAssertion<IPAddress>( nameof(IPAddress.IsIPv6SiteLocal), CustomName = "IsNotIPv6SiteLocal", NegateLogic = true)]

// Custom helper methods
[CreateAssertion<IPAddress>( typeof(IPAddressAssertionExtensions), nameof(IsIPv4))]
[CreateAssertion<IPAddress>( typeof(IPAddressAssertionExtensions), nameof(IsIPv4), CustomName = "IsNotIPv4", NegateLogic = true)]

[CreateAssertion<IPAddress>( typeof(IPAddressAssertionExtensions), nameof(IsIPv6))]
[CreateAssertion<IPAddress>( typeof(IPAddressAssertionExtensions), nameof(IsIPv6), CustomName = "IsNotIPv6", NegateLogic = true)]

[CreateAssertion<IPAddress>( typeof(IPAddressAssertionExtensions), nameof(IsLoopback))]
[CreateAssertion<IPAddress>( typeof(IPAddressAssertionExtensions), nameof(IsLoopback), CustomName = "IsNotLoopback", NegateLogic = true)]

[CreateAssertion<IPAddress>( typeof(IPAddressAssertionExtensions), nameof(IsPrivate))]
[CreateAssertion<IPAddress>( typeof(IPAddressAssertionExtensions), nameof(IsPrivate), CustomName = "IsPublic", NegateLogic = true)]
public static partial class IPAddressAssertionExtensions
{
    internal static bool IsIPv4(IPAddress address) => address.AddressFamily == AddressFamily.InterNetwork;
    internal static bool IsIPv6(IPAddress address) => address.AddressFamily == AddressFamily.InterNetworkV6;
    internal static bool IsLoopback(IPAddress address) => IPAddress.IsLoopback(address);

    internal static bool IsPrivate(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10)
            {
                return true;
            }

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            // 127.0.0.0/8 (loopback)
            if (bytes[0] == 127)
            {
                return true;
            }
        }
        else if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Check for IPv6 private addresses (fc00::/7)
            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xfe) == 0xfc;
        }
        return false;
    }
}
