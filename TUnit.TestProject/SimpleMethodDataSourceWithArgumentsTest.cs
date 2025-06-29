namespace TUnit.TestProject;

public class SimpleMethodDataSourceWithArgumentsTest
{
    [Test]
    [MethodDataSource(nameof(GetData), Arguments = new object[] { 5 })]
    public async Task TestWithArguments(int value)
    {
        // Test should receive 5, 10, 15
        await Assert.That(value % 5).IsEqualTo(0);
    }

    public static int[] GetData(int multiplier)
    {
        return new[] { 1 * multiplier, 2 * multiplier, 3 * multiplier };
    }
}
