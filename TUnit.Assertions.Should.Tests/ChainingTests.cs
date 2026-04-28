using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

public class ChainingTests
{
    [Test]
    public async Task And_two_passes()
    {
        await 5.Should().BeEqualTo(5).And.NotBeEqualTo(7);
    }

    [Test]
    public async Task And_three_passes()
    {
        await 5.Should().BeEqualTo(5).And.NotBeEqualTo(7).And.BeBetween(1, 10);
    }

    [Test]
    public async Task And_first_fails_throws()
    {
        await Assert.That(async () => await 5.Should().BeEqualTo(99).And.NotBeEqualTo(7))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task And_second_fails_throws()
    {
        await Assert.That(async () => await 5.Should().BeEqualTo(5).And.BeEqualTo(99))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Or_first_passes_short_circuits()
    {
        await 5.Should().BeEqualTo(5).Or.BeEqualTo(99);
    }

    [Test]
    public async Task Or_second_passes()
    {
        await 5.Should().BeEqualTo(99).Or.BeEqualTo(5);
    }

    [Test]
    public async Task Or_both_fail_throws()
    {
        await Assert.That(async () => await 5.Should().BeEqualTo(99).Or.BeEqualTo(100))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Mixed_And_Or_throws()
    {
        await Assert.That(async () => await 5.Should().BeEqualTo(5).And.BeEqualTo(5).Or.BeEqualTo(7))
            .Throws<MixedAndOrAssertionsException>();
    }

    [Test]
    public async Task Chain_keeps_Should_naming_throughout()
    {
        // After .And the source is ShouldContinuation<T>: IShouldSource<T>; only Should-flavored
        // extensions should resolve.
        var list = new List<int> { 1, 2, 3 };
        await list.Should().Contain(1).And.Contain(2).And.NotContain(99);
    }

    [Test]
    public async Task Because_propagates_to_failure_message()
    {
        var ex = await Assert.That(async () =>
                await 5.Should().BeEqualTo(99).Because("business rule X"))
            .Throws<AssertionException>();
        await Assert.That(ex.Message).Contains("business rule X");
    }
}
