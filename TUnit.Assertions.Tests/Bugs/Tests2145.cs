using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Tests.Bugs;

public class Tests2145
{
    [Test]
    public async Task TestFailMessage()
    {
        await Assert.That(async () =>
            {
                var val = "hello";

                using var _ = Assert.Multiple();
                await Assert.That(val).IsEqualTo("world");
                await Assert.That(val).IsEqualTo("World");
            }).Throws<AssertionException>()
            .WithMessage(
                """
                Expected val to be equal to "world"
                
                but found "hello" which differs at index 0:
                    ↓
                   "hello"
                   "world"
                    ↑

                at Assert.That(val).IsEqualTo("world")

                Expected val to be equal to "World"
                
                but found "hello" which differs at index 0:
                    ↓
                   "hello"
                   "World"
                    ↑

                at Assert.That(val).IsEqualTo("World")
                """
            );
    }
}
