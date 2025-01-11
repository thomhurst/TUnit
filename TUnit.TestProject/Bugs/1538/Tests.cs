namespace TUnit.TestProject.Bugs._1538;

public class Tests
{
    [Test]
    [MethodDataSource(nameof(T5_DeconstructTestDataSource))]
    public void T5_Deconstruct_Should_ReturnAllResults(bool expectedSuccess, string? expectedError,
        string? expectedT0, string? expectedT1, string? expectedT2, string? expectedT3, string? expectedT4, string? expectedT5)
    {
        Console.WriteLine();
    }

    public static
        IEnumerable<(bool success, string? expectedError, string? expectedT0, string? expectedT1, string? expectedT2,
            string? expectedT3, string? expectedT4, string? expectedT5)>
        T5_DeconstructTestDataSource()
    {
        yield return (true, null, "This is a success", null, null, null, null, null);
        yield return (false, "This is a failure", null, null, null, null, null, null);
    }
}