using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataSourceDrivenTestArgumentsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.NoTestDataSourceProvided,
            Rules.NoDataSourceMethodFound,
            Rules.TestDataSourceMethodNotStatic,
            Rules.TestDataSourceMethodNotPublic,
            Rules.TestDataSourceMethodAbstract,
            Rules.TestDataSourceMethodNotParameterless,
            Rules.WrongArgumentTypeTestDataSource,
            Rules.TestDataSourceMethodNotReturnsNothing
        );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.MethodDeclaration);

        // Check other 'context.Register...' methods that might be helpful for your purposes.
    }

    /// <summary>
    /// Executed for each Syntax Node with 'SyntaxKind' is 'ClassDeclaration'.
    /// </summary>
    /// <param name="context">Operation context.</param>
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        // The Roslyn architecture is based on inheritance.
        // To get the required metadata, we should match the 'Node' object to the particular type: 'ClassDeclarationSyntax'.
        if (context.Node is not MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax)
            is not { } methodSymbol)
        {
            return;
        }

        var attributes = methodSymbol.GetAttributes();
        
        foreach (var dataSourceDrivenAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                == "global::TUnit.Core.DataSourceDrivenTestAttribute"))
        {
            CheckAttributeAgainstMethod(context, methodSymbol, dataSourceDrivenAttribute);
        }
    }

    private void CheckAttributeAgainstMethod(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol,
        AttributeData dataSourceDrivenAttribute)
    {
        var methodParameterType = methodSymbol.Parameters.Select(x => x.Type).FirstOrDefault();
        var methodContainingTestData = FindMethodContainingTestData(context, dataSourceDrivenAttribute, methodSymbol.ContainingType);
        
        if (methodContainingTestData is null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.NoDataSourceMethodFound,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.ReturnsVoid)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TestDataSourceMethodNotReturnsNothing,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        if (!methodContainingTestData.IsStatic)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TestDataSourceMethodNotStatic,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TestDataSourceMethodNotPublic,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.Parameters.Any())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TestDataSourceMethodNotParameterless,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }

        var argumentType = methodContainingTestData.ReturnType;

        var enumerableOfMethodType = CreateEnumerableOfType(context, methodParameterType!);

        if (SymbolEqualityComparer.Default.Equals(argumentType, methodParameterType)
            || SymbolEqualityComparer.Default.Equals(argumentType, enumerableOfMethodType))
        {
            return;
        }
        
        context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.NoTestDataSourceProvided,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    argumentType,
                    methodParameterType)
            );
    }

    private static ITypeSymbol CreateEnumerableOfType(SyntaxNodeAnalysisContext context, ITypeSymbol typeSymbol)
    {
        return context.SemanticModel.Compilation
            .GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
            .Construct(typeSymbol);
    }

    private IMethodSymbol? FindMethodContainingTestData(SyntaxNodeAnalysisContext context, AttributeData dataSourceDrivenAttribute,
        INamedTypeSymbol classContainingTest)
    {
        if (dataSourceDrivenAttribute.ConstructorArguments.Length == 1)
        {
            var methodName = dataSourceDrivenAttribute.ConstructorArguments.First().Value as string;
            return classContainingTest.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == methodName);
        }
        else
        {
            var methodName = dataSourceDrivenAttribute.ConstructorArguments[1].Value as string;

            var @class =
                dataSourceDrivenAttribute.ConstructorArguments[0].Value as INamedTypeSymbol ?? classContainingTest;
            
            return @class?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == methodName);
        }
    }
}