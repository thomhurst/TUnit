using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataDrivenTestArgumentsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.InvalidDataAssertion, Rules.NoDataProvidedAssertion);

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
                                                == "global::TUnit.Core.DataDrivenTestAttribute"))
        {
            CheckAttributeAgainstMethod(context, methodSymbol, dataDrivenTestAttribute);
        }
    }

    private void CheckAttributeAgainstMethod(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol,
        AttributeData dataDrivenTestAttribute)
    {
        if (!dataDrivenTestAttribute.ConstructorArguments.Any())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoDataProvidedAssertion,
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }

        var methodParameterTypes = methodSymbol.Parameters.Select(x => x.Type).ToList();
        var attributeTypesPassedIn = dataDrivenTestAttribute.ConstructorArguments.First().Values.Select(x => x.Type).ToList();

        if (methodParameterTypes.Count != attributeTypesPassedIn.Count)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.InvalidDataAssertion,
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    string.Join(", ", attributeTypesPassedIn.Select(x => x?.ToDisplayString())),
                    string.Join(", ", methodParameterTypes.Select(x => x?.ToDisplayString())))
            );
            return;
        }
        
        for (var i = 0; i < methodParameterTypes.Count; i++)
        {
            var methodParameterType = methodParameterTypes[i];
            var attributeArgumentType = attributeTypesPassedIn[i];

            if (IsEnumAndInteger(methodParameterType, attributeArgumentType))
            {
                continue;
            }
            
            if (!context.Compilation.HasImplicitConversion(attributeArgumentType, methodParameterType))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.InvalidDataAssertion,
                        dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                        attributeArgumentType?.ToDisplayString(),
                        methodParameterType?.ToDisplayString())
                );
            }
        }
    }

    private bool IsEnumAndInteger(ITypeSymbol type1, ITypeSymbol? type2)
    {
        if (type1.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "int")
        {
            return type2?.TypeKind == TypeKind.Enum;
        }
        
        if (type2?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "int")
        {
            return type1.TypeKind == TypeKind.Enum;
        }

        return false;
    }
}