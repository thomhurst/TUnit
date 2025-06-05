using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._1538;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [MethodDataSource(nameof(EightItems))]
    public void Eight_Args(bool expectedSuccess, string? expectedError,
        string? expectedT0, string? expectedT1, string? expectedT2, string? expectedT3, string? expectedT4, string? expectedT5)
    {
        Console.WriteLine();
    }

    [Test]
    [MethodDataSource(nameof(SixteenItems))]
    public void SixteenArgs(
        bool expectedSuccess,
        string? item1,
        string? item2,
        string? item3,
        string? item4,
        string? item5,
        string? item6,
        string? item7,
        string? item8,
        string? item9,
        string? item10,
        string? item11,
        string? item12,
        string? item13,
        string? item14,
        string? item15
        )
    {
        Console.WriteLine();
    }

    public static
        IEnumerable<(bool success, string? expectedError, string? expectedT0, string? expectedT1, string? expectedT2,
            string? expectedT3, string? expectedT4, string? expectedT5)>
        EightItems()
    {
        yield return (true, null, "This is a success", null, null, null, null, null);
        yield return (false, "This is a failure", null, null, null, null, null, null);
    }

    public static
        IEnumerable<(bool success,
            string? item1,
            string? item2,
            string? item3,
            string? item4,
            string? item5,
            string? item6,
            string? item7,
            string? item8,
            string? item9,
            string? item10,
            string? item11,
            string? item12,
            string? item13,
            string? item14,
            string? item15
            )>
        SixteenItems()
    {
        yield return (
            true,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty
            );
    }
}