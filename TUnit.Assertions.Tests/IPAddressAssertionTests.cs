using System.Net;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class IPAddressAssertionTests
{
    [Test]
    public async Task Test_IPAddress_IsIPv4MappedToIPv6()
    {
        // Create an IPv4-mapped IPv6 address
        var ipv4 = IPAddress.Parse("192.168.1.1");
        var mappedAddress = ipv4.MapToIPv6();
        await Assert.That(mappedAddress).IsIPv4MappedToIPv6();
    }

    [Test]
    public async Task Test_IPAddress_IsIPv4MappedToIPv6_Loopback()
    {
        var ipv4 = IPAddress.Loopback;
        var mappedAddress = ipv4.MapToIPv6();
        await Assert.That(mappedAddress).IsIPv4MappedToIPv6();
    }

    [Test]
    public async Task Test_IPAddress_IsNotIPv4MappedToIPv6_PureIPv6()
    {
        var address = IPAddress.IPv6Loopback;
        await Assert.That(address).IsNotIPv4MappedToIPv6();
    }

    [Test]
    public async Task Test_IPAddress_IsNotIPv4MappedToIPv6_IPv4()
    {
        var address = IPAddress.Parse("192.168.1.1");
        await Assert.That(address).IsNotIPv4MappedToIPv6();
    }

    [Test]
    public async Task Test_IPAddress_IsIPv6LinkLocal()
    {
        // Link-local addresses start with fe80::
        var address = IPAddress.Parse("fe80::1");
        await Assert.That(address).IsIPv6LinkLocal();
    }

    [Test]
    public async Task Test_IPAddress_IsIPv6LinkLocal_WithScopeId()
    {
        var address = IPAddress.Parse("fe80::215:5dff:fe00:402");
        await Assert.That(address).IsIPv6LinkLocal();
    }

    [Test]
    public async Task Test_IPAddress_IsNotIPv6LinkLocal()
    {
        var address = IPAddress.Parse("2001:db8::1");
        await Assert.That(address).IsNotIPv6LinkLocal();
    }

    [Test]
    public async Task Test_IPAddress_IsIPv6Multicast()
    {
        // Multicast addresses start with ff00::
        var address = IPAddress.Parse("ff02::1");
        await Assert.That(address).IsIPv6Multicast();
    }

    [Test]
    public async Task Test_IPAddress_IsIPv6Multicast_AllNodes()
    {
        var address = IPAddress.Parse("ff02::1"); // All nodes multicast
        await Assert.That(address).IsIPv6Multicast();
    }

    [Test]
    public async Task Test_IPAddress_IsNotIPv6Multicast()
    {
        var address = IPAddress.Parse("2001:db8::1");
        await Assert.That(address).IsNotIPv6Multicast();
    }

    [Test]
    public async Task Test_IPAddress_IsIPv6SiteLocal()
    {
        // Site-local addresses start with fec0:: (deprecated but still testable)
        var address = IPAddress.Parse("fec0::1");
        await Assert.That(address).IsIPv6SiteLocal();
    }

    [Test]
    public async Task Test_IPAddress_IsIPv6SiteLocal_Range()
    {
        var address = IPAddress.Parse("fec0:0:0:1::1");
        await Assert.That(address).IsIPv6SiteLocal();
    }

    [Test]
    public async Task Test_IPAddress_IsNotIPv6SiteLocal()
    {
        var address = IPAddress.Parse("2001:db8::1");
        await Assert.That(address).IsNotIPv6SiteLocal();
    }

    [Test]
    public async Task Test_IPAddress_IsIPv6Teredo()
    {
        // Teredo addresses start with 2001:0000::
        var address = IPAddress.Parse("2001:0000:4136:e378:8000:63bf:3fff:fdd2");
        await Assert.That(address).IsIPv6Teredo();
    }

    [Test]
    public async Task Test_IPAddress_IsIPv6Teredo_Alternate()
    {
        var address = IPAddress.Parse("2001:0:5ef5:79fd:0:0:0:1");
        await Assert.That(address).IsIPv6Teredo();
    }

    [Test]
    public async Task Test_IPAddress_IsNotIPv6Teredo()
    {
        var address = IPAddress.Parse("2001:db8::1");
        await Assert.That(address).IsNotIPv6Teredo();
    }

    [Test]
    public async Task Test_IPAddress_IsNotIPv6Teredo_LinkLocal()
    {
        var address = IPAddress.Parse("fe80::1");
        await Assert.That(address).IsNotIPv6Teredo();
    }
}
