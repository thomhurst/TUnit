namespace TUnit.Assertions.Tests;

public class EnumerableTests
{
    [Test]
    public async Task Test1()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).Contains(1);
    }
}