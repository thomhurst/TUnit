using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Should;
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
}
