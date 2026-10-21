using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// Pins where <see cref="AssertionMethodGenerator"/> reports TU0001 when an [AssertionFrom] attribute names a method
/// that does not exist.
/// </summary>
internal class MissingMethodDiagnosticTests
{
    private const string MissingMethodSource =
        """
        using TUnit.Assertions.Attributes;

        namespace Missing;

        public static class Methods
        {
            public static bool Exists(string value) => true;
        }

        [AssertionFrom<string>(typeof(Methods), "DoesNotExist")]
        public static partial class MissingAssertions;
        """;

    [Test]
    public async Task Is_Located_In_The_Attribute_Of_The_Current_Syntax_Tree()
    {
        var (_, diagnostics) = AssertionMethodGeneratorRunner.Run(MissingMethodSource);

        var diagnostic = diagnostics.Single(d => d.Id == "TU0001");

        // A location in the compilation's own tree keeps #pragma and per-file .editorconfig severities working,
        // and lets the IDE navigate to it; a path-only location does neither
        await Assert.That(diagnostic.Location.IsInSource).IsTrue();
        await Assert.That(diagnostic.Location.SourceTree?.FilePath).IsEqualTo(AssertionMethodGeneratorRunner.InputPath);
        await Assert.That(diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan))
            .IsEqualTo("""AssertionFrom<string>(typeof(Methods), "DoesNotExist")""");
    }
}
