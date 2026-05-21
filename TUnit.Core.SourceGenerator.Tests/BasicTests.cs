using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

internal class BasicTests : TestsBase
{
    private static readonly string InputFile = Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "BasicTests.cs");

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

            await Assert.That(generatedCode).Contains($"LineNumber = {lineSpan.StartLinePosition.Line + 1},");
            await Assert.That(generatedCode).Contains($"StartColumnNumber = {lineSpan.StartLinePosition.Character + 1},");
            await Assert.That(generatedCode).Contains($"EndLineNumber = {lineSpan.EndLinePosition.Line + 1},");
            await Assert.That(generatedCode).Contains($"EndColumnNumber = {lineSpan.EndLinePosition.Character + 1},");
        });
}
