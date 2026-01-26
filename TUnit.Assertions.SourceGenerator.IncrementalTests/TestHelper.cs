using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

internal static class TestHelper
{
    private static readonly GeneratorDriverOptions EnableIncrementalTrackingDriverOptions = new(
        IncrementalGeneratorOutputKind.None,
        trackIncrementalGeneratorSteps: true
    );

    internal static GeneratorDriver GenerateTracked(Compilation compilation)
    {
        var generator = new MethodAssertionGenerator();

        var driver = CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            driverOptions: EnableIncrementalTrackingDriverOptions
        );
        return driver.RunGenerators(compilation);
    }

    internal static CSharpCompilation ReplaceMemberDeclaration(
        CSharpCompilation compilation,
        string memberName,
        string newMember
    )
    {
        var syntaxTree = compilation.SyntaxTrees.Single();
        var memberDeclaration = syntaxTree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Single(x => x.Identifier.Text == memberName);
        var updatedMemberDeclaration = SyntaxFactory.ParseMemberDeclaration(newMember)!;

        var newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceNode(memberDeclaration, updatedMemberDeclaration);
        var newTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);

        return compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), newTree);
    }

    internal static CSharpCompilation ReplaceLocalDeclaration(
        CSharpCompilation compilation,
        string variableName,
        string newDeclaration
    )
    {
        var syntaxTree = compilation.SyntaxTrees.Single();

        var memberDeclaration = syntaxTree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<LocalDeclarationStatementSyntax>()
            .Single(x => x.Declaration.Variables.Any(x => x.Identifier.ToString() == variableName));
        var updatedMemberDeclaration = SyntaxFactory.ParseStatement(newDeclaration)!;

        var newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceNode(memberDeclaration, updatedMemberDeclaration);
        var newTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);

        return compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), newTree);
    }

    internal static CSharpCompilation ReplaceMethodDeclaration(
        CSharpCompilation compilation,
        string methodName,
        string newDeclaration
    )
    {
        var syntaxTree = compilation.SyntaxTrees.Single();

        var memberDeclaration = syntaxTree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First(x => x.Identifier.Text == methodName);
        var updatedMemberDeclaration = SyntaxFactory.ParseMemberDeclaration(newDeclaration)!;

        var newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceNode(memberDeclaration, updatedMemberDeclaration);
        var newTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);

        return compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), newTree);
    }


    internal static void AssertRunReasons(
        GeneratorDriver driver,
        IncrementalGeneratorRunReasons reasons,
        int outputIndex = 0
    )
    {
        var runResult = driver.GetRunResult().Results[0];

        AssertRunReason(runResult, MethodAssertionGenerator.BuildAssertion, reasons.BuildMethodAssertionStep, outputIndex);
    }

    private static void AssertRunReason(
        GeneratorRunResult runResult,
        string stepName,
        IncrementalStepRunReason expectedStepReason,
        int outputIndex
    )
    {
        var actualStepReason = runResult
            .TrackedSteps[stepName]
            .SelectMany(x => x.Outputs)
            .ElementAt(outputIndex)
            .Reason;

        if (actualStepReason != expectedStepReason)
        {
            throw new Exception($"Incremental generator step {stepName} at index {outputIndex} failed " +
                                $"with the expected reason: {expectedStepReason}, with the actual reason: {actualStepReason}.");
        }
    }
}

internal record IncrementalGeneratorRunReasons(
    IncrementalStepRunReason BuildMethodAssertionStep,
    IncrementalStepRunReason ReportDiagnosticsStep
)
{
    public static readonly IncrementalGeneratorRunReasons New = new(
        IncrementalStepRunReason.New,
        IncrementalStepRunReason.New
    );

    public static readonly IncrementalGeneratorRunReasons Cached = new(
        // compilation step should always be modified as each time a new compilation is passed
        IncrementalStepRunReason.Cached,
        IncrementalStepRunReason.Cached
    );

    public static readonly IncrementalGeneratorRunReasons Modified = Cached with
    {
        ReportDiagnosticsStep = IncrementalStepRunReason.Modified,
        BuildMethodAssertionStep = IncrementalStepRunReason.Modified,
    };

    public static readonly IncrementalGeneratorRunReasons ModifiedSource = Cached with
    {
        ReportDiagnosticsStep = IncrementalStepRunReason.Unchanged,
        BuildMethodAssertionStep = IncrementalStepRunReason.Modified,
    };
}

