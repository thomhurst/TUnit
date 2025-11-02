namespace TUnit.Assertions.Tests.Bugs;

public class Tests2145
{
    [Test]
    public async Task TestFailMessage()
    {
        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            {
                var val = "hello";

                using var _ = Assert.Multiple();
                await Assert.That(val).IsEqualTo("world");
                await Assert.That(val).IsEqualTo("World");
            });

        var expectedMessage = """
                Expected to be equal to "world"
                but found "hello"

                at Assert.That(val).IsEqualTo("world")

                Expected to be equal to "World"
                but found "hello"

                at Assert.That(val).IsEqualTo("World")
                """;

        await Assert.That(exception!.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage.NormalizeLineEndings());
    }
}
