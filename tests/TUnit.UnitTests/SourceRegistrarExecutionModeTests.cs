namespace TUnit.UnitTests;

/// <summary>
/// Regression test for #7089. This project references TUnit.Playwright, which is built with the
/// source generator. In reflection mode its generated module initializer first runs during
/// discovery, after the engine has selected the mode, and used to set
/// <see cref="SourceRegistrar.IsEnabled"/> back to <c>true</c>. Closed generic test classes then
/// failed with "Cannot use reflection-based hook discovery when source generation is enabled".
/// Run this project with and without <c>--reflection</c> to cover both modes.
/// </summary>
[GenerateGenericTest(typeof(int))]
public class SourceRegistrarExecutionModeGenericTests<T>
{
    [Test]
    public async Task Closed_Generic_Test_Class_Runs()
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
    }
}
