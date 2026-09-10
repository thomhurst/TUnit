using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

public class StringTests
{
    [Test]
    public async Task BeEqualTo()
    {
        await "hello".Should().BeEqualTo("hello");
    }

    [Test]
    public async Task NotBeEqualTo()
    {
        await "hello".Should().NotBeEqualTo("world");
    }

    [Test]
    public async Task Contain()
    {
        await "hello world".Should().Contain("world");
    }

    [Test]
    public async Task NotContain()
    {
        await "hello world".Should().NotContain("xyz");
    }

    [Test]
    public async Task StartWith()
    {
        await "hello world".Should().StartWith("hello");
    }

    [Test]
    public async Task NotStartWith()
    {
        await "hello world".Should().NotStartWith("xyz");
    }

    [Test]
    public async Task EndWith()
    {
        await "hello world".Should().EndWith("world");
    }

    [Test]
    public async Task NotEndWith()
    {
        await "hello world".Should().NotEndWith("xyz");
    }

    [Test]
    public async Task NotMatch()
    {
        await "hello".Should().NotMatch(@"\d+");
    }

    [Test]
    public async Task Failure_includes_method_name_in_expression()
    {
        var ex = await Assert.That(async () => await "hello".Should().Contain("xyz"))
            .Throws<TUnit.Assertions.Exceptions.AssertionException>();
        await Assert.That(ex.Message).Contains(".Should().Contain(");
    }
}
