namespace TUnit.TestProject.Bugs._7108;

/// <summary>
/// No overload of the data method accepts the Arguments. TUnit0001 reports that at compile time; with it
/// suppressed, both modes still discover the generic test and fail it when the data method is called, rather than
/// dropping it. Not an [EngineTest]: the test fails by design, and Issue7108Tests checks it in each mode.
/// </summary>
public class NoMatchingOverloadDataSourceTests
{
    public static IEnumerable<int> Rows(int value)
    {
        yield return value;
    }

#pragma warning disable TUnit0001
    [Test]
    [MethodDataSource(nameof(Rows), Arguments = new object[] { "not an int" })]
    public async Task Generic<T>(T value)
    {
        await Assert.That((object?)value).IsNotNull();
    }
#pragma warning restore TUnit0001
}
