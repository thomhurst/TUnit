using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

internal static class TestHelper
{
    private static readonly GeneratorDriverOptions _enableIncrementalTrackingDriverOptions = new(
        IncrementalGeneratorOutputKind.None,
        trackIncrementalGeneratorSteps: true
    );

    internal static GeneratorDriver GenerateTracked<TSourceGenerator>(Compilation compilation)
        where TSourceGenerator : IIncrementalGenerator, new()
    {
        var generator = new TSourceGenerator();

        var driver = CSharpGeneratorDriver.Create(
            [ generator.AsSourceGenerator() ],
            driverOptions: _enableIncrementalTrackingDriverOptions
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
        var updatedMemberDeclaration = SyntaxFactory.ParseStatement(newDeclaration);

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

    public static void AssertRunReason(
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
    IncrementalStepRunReason BuildStep,
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
        BuildStep = IncrementalStepRunReason.Modified,
    };

    public static readonly IncrementalGeneratorRunReasons ModifiedSource = Cached with
    {
        ReportDiagnosticsStep = IncrementalStepRunReason.Unchanged,
        BuildStep = IncrementalStepRunReason.Modified,
    };
}

