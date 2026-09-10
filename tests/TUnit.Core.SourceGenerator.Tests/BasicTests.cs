using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class BasicTests : TestsBase
{
    private static readonly string InputFile = Path.Combine(Git.TestsDirectory.FullName, "TUnit.TestProject", "BasicTests.cs");

    [Test]
    public Task Test() => RunTest(InputFile,
        new RunTestOptions
        {
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            var source = await File.ReadAllTextAsync(InputFile);
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var firstMethod = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First(x => x.Identifier.ValueText == "SynchronousTest");

            var lineSpan = firstMethod.GetLocation().GetLineSpan();
            var generatedCode = string.Join(Environment.NewLine, generatedFiles);

            // TestEntry source locations are emitted as TestEntryFactory.Create named arguments
            await Assert.That(generatedCode).Contains($"lineNumber: {lineSpan.StartLinePosition.Line + 1},");

            // Column/end spans are environment-sensitive (zero when the span is unavailable) and
            // the generator omits the argument entirely when zero, so assert values only when emitted.
            if (generatedCode.Contains("startColumnNumber: "))
            {
                await Assert.That(generatedCode).Contains($"startColumnNumber: {lineSpan.StartLinePosition.Character + 1},");
                await Assert.That(generatedCode).Contains($"endLineNumber: {lineSpan.EndLinePosition.Line + 1},");
                await Assert.That(generatedCode).Contains($"endColumnNumber: {lineSpan.EndLinePosition.Character + 1},");
            }
        });
}
