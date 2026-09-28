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

    internal static CSharpCompilation ReplaceTypeDeclaration(
        CSharpCompilation compilation,
        string typeName,
        string newMember
    )
    {
        var syntaxTree = compilation.SyntaxTrees.Single();
        var memberDeclaration = syntaxTree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Single(x => x.Identifier.Text == typeName);
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

    internal static CSharpCompilation ReplacePropertyDeclaration(
        CSharpCompilation compilation,
        string propertyName,
        string newDeclaration
    )
    {
        var syntaxTree = compilation.SyntaxTrees.Single();

        var memberDeclaration = syntaxTree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .First(x => x.Identifier.Text == propertyName);
        var updatedMemberDeclaration = SyntaxFactory.ParseMemberDeclaration(newDeclaration)!;

        var newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceNode(memberDeclaration, updatedMemberDeclaration);
        var newTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);

        return compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), newTree);
    }

    /// <summary>
    /// Asserts every output of the tracked step <paramref name="stepName"/> has one of the
    /// <paramref name="allowedReasons"/>, and that the step produced at least one output.
    /// </summary>
    public static void AssertAllRunReasons(
        GeneratorRunResult runResult,
        string stepName,
        params IncrementalStepRunReason[] allowedReasons
    )
    {
        var outputs = runResult.TrackedSteps[stepName]
            .SelectMany(x => x.Outputs)
            .ToArray();

        Xunit.Assert.NotEmpty(outputs);
        foreach (var output in outputs)
        {
            Xunit.Assert.Contains(output.Reason, allowedReasons);
        }
    }

    /// <summary>
    /// Asserts at least one output of the tracked step <paramref name="stepName"/> has <paramref name="expectedReason"/>.
    /// </summary>
    public static void AssertAnyRunReason(
        GeneratorRunResult runResult,
        string stepName,
        IncrementalStepRunReason expectedReason
    )
    {
        var reasons = runResult.TrackedSteps[stepName]
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .ToArray();

        Xunit.Assert.Contains(expectedReason, reasons);
    }

    /// <summary>
    /// Asserts no source output was re-run, i.e. nothing was regenerated.
    /// </summary>
    public static void AssertSourceOutputsCached(GeneratorRunResult runResult)
    {
        var outputs = runResult.TrackedOutputSteps
            .SelectMany(x => x.Value)
            .SelectMany(x => x.Outputs)
            .ToArray();

        Xunit.Assert.NotEmpty(outputs);
        foreach (var output in outputs)
        {
            Xunit.Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
        }
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

    public static readonly IncrementalGeneratorRunReasons Unchanged = new(
        IncrementalStepRunReason.Unchanged,
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

