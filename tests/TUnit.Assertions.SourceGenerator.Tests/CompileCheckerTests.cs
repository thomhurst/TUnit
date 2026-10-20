namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// Guards the compile gate itself, on every target framework the project runs on
/// (including the Windows-only net472 leg): generated code that uses the attributes
/// a .NET Framework consumer gets from Polyfill must compile, and a genuine error must
/// still be reported.
/// </summary>
internal class CompileCheckerTests
{
    [Test]
    public Task Attributes_Emitted_By_The_Generators_Resolve_On_Every_Target_Framework() =>
        CompileChecker.AssertNoErrors(
        [
            """
            namespace Sample;

            [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "Test")]
            public static class Extensions
            {
                [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050")]
                public static string Describe<T>(T value, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? valueExpression = null)
                    => valueExpression ?? string.Empty;
            }
            """,
        ]);

    [Test]
    public async Task Reports_Genuine_Errors()
    {
        var exception = await Assert.That(() => CompileChecker.AssertNoErrors(["public class Broken { public void M() { int x = \"text\"; } }"]))
            .Throws<Exception>();

        await Assert.That(exception!.Message).Contains("CS0029");
    }
}
