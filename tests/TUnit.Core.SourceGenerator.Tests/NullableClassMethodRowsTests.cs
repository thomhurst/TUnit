namespace TUnit.Core.SourceGenerator.Tests;

internal class NullableClassMethodRowsTests : TestsBase
{
    [Test]
    public Task Generates_Nullable_Class_From_Method_Rows() => RunTest(Path.Combine(Git.TestsDirectory.FullName,
        "TUnit.TestProject", "Bugs", "_7573", "NullableClassMethodRows.cs"), async generatedFiles =>
    {
        foreach (var method in new[] { "Value", "NullableValue", "Factory", "Tuple" })
        {
            await Assert.That(generatedFiles.Any(file => file.Contains($"NullableClassMethodRows_T_{method}_")
                && file.Contains("NullableClassMethodRows<int>"))).IsTrue();
        }
    });
}
