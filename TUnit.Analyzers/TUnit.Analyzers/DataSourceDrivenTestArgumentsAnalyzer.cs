using System;
using System.Collections.Generic;
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
        ImmutableArray.Create(Rules.InvalidDataSourceAssertion, Rules.NoDataSourceMethodFoundAssertion);

    public override void Initialize(AnalysisContext context)
    {
        // You must call this method to avoid analyzing generated code.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // You must call this method to enable the Concurrent Execution.
        context.EnableConcurrentExecution();

        // Subscribe to the Syntax Node with the appropriate 'SyntaxKind' (ClassDeclaration) action.
        // To figure out which Syntax Nodes you should choose, consider installing the Roslyn syntax tree viewer plugin Rossynt: https://plugins.jetbrains.com/plugin/16902-rossynt/
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
        
        foreach (var dataDrivenTestAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                == "global::TUnit.Core.DataSourceDrivenTestAttribute"))
        {
            CheckAttributeAgainstMethod(context, methodSymbol, dataDrivenTestAttribute);
        }
    }

    private void CheckAttributeAgainstMethod(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol,
        AttributeData dataDrivenTestAttribute)
    {
        var methodParameterType = methodSymbol.Parameters.Select(x => x.Type).FirstOrDefault();
        var methodContainingTestData = FindMethodContainingTestData(context, dataDrivenTestAttribute, methodSymbol.ContainingType);

        if (methodContainingTestData is null 
            || methodContainingTestData.ReturnsVoid
            || !methodContainingTestData.IsStatic
            || methodContainingTestData.DeclaredAccessibility != Accessibility.Public
            || methodContainingTestData.Parameters.Any()
            )
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.NoDataSourceMethodFoundAssertion,
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
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
                    Rules.InvalidDataSourceAssertion,
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    argumentType,
                    methodParameterType)
            );
    }

    private static ITypeSymbol CreateEnumerableOfType(SyntaxNodeAnalysisContext context, ITypeSymbol typeSymbol)
    {
        return context.SemanticModel.Compilation
            .GetTypeByMetadataName(typeof(IEnumerable<>).FullName!)!
            .Construct(typeSymbol);
    }

    private IMethodSymbol? FindMethodContainingTestData(SyntaxNodeAnalysisContext context, AttributeData dataDrivenTestAttribute,
        INamedTypeSymbol classContainingTest)
    {
        if (dataDrivenTestAttribute.ConstructorArguments.Length == 1)
        {
            var methodName = dataDrivenTestAttribute.ConstructorArguments.First().Value as string;
            return classContainingTest.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == methodName);
        }
        else
        {
            var methodName = dataDrivenTestAttribute.ConstructorArguments[1].Value as string;

            var @class =
                dataDrivenTestAttribute.ConstructorArguments[0].Value is not Type classType
                    ? classContainingTest
                    : context.Compilation.GetTypeByMetadataName(classType.AssemblyQualifiedName ?? classType.FullName!);
            
            return @class?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == methodName);
        }
    }
}