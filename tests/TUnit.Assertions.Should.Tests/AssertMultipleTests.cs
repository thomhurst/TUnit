using TUnit.Assertions.Core;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Core;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

public class AssertMultipleTests
{
    [Test]
    public async Task All_pass_inside_Multiple()
    {
        using (Assert.Multiple())
        {
            await 5.Should().BeEqualTo(5);
            await "hello".Should().BeEqualTo("hello");
            await new[] { 1, 2 }.Should().Contain(1);
        }
    }

    [Test]
    public async Task Multiple_failures_aggregate()
    {
        var ex = await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                await 5.Should().BeEqualTo(99);
                await "hello".Should().BeEqualTo("world");
            }
        }).Throws<Exception>();

        // Both failures should appear in the aggregated message.
        await Assert.That(ex.Message).Contains("BeEqualTo");
    }

    [Test]
    public async Task Single_failure_in_Multiple_still_throws()
    {
        await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                await 5.Should().BeEqualTo(5);
                await 5.Should().BeEqualTo(99);
            }
        }).Throws<Exception>();
    }

    [Test]
    public async Task AssertAsync_can_be_used_with_Task_WhenAll()
    {
#pragma warning disable TUnitAssertions0002
        var tasks = new List<Task>
        {
            5.Should().BeEqualTo(5).AssertAsync(),
            "hello".Should().BeEqualTo("hello").AssertAsync(),
            new[] { 1, 2 }.Should().Contain(1).AssertAsync(),
        };
#pragma warning restore TUnitAssertions0002

        await Task.WhenAll(tasks);
    }

    [Test]
    public async Task AssertAsync_surfaces_failure_when_awaited_via_WhenAll()
    {
#pragma warning disable TUnitAssertions0002
        var tasks = new List<Task>
        {
            5.Should().BeEqualTo(5).AssertAsync(),
            5.Should().BeEqualTo(99).AssertAsync(),
        };
#pragma warning restore TUnitAssertions0002

        await Assert.That(async () => await Task.WhenAll(tasks)).Throws<Exception>();
    }

    [Test]
    public async Task IAssertion_AssertAsync_executes_via_interface()
    {
#pragma warning disable TUnitAssertions0002
        ShouldAssertion<int> chain = 5.Should().BeEqualTo(5);
#pragma warning restore TUnitAssertions0002
        IAssertion erased = chain;

        await erased.AssertAsync();
    }
}
