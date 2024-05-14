using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestRequiresDataAttributesAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.ConflictingTestAttributes);

    public override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.MethodDeclaration);
    }
    
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    { 
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

        Compare(context, attributes, 
            WellKnown.AttributeFullyQualifiedClasses.CombinativeTest,
            WellKnown.AttributeFullyQualifiedClasses.CombinativeValues,
            Rules.RequiredPair_Attributes_CombinativeTest_CombinativeValues,
            methodSymbol.Locations.FirstOrDefault());
        
        Compare(context, attributes, 
            WellKnown.AttributeFullyQualifiedClasses.DataDrivenTest,
            WellKnown.AttributeFullyQualifiedClasses.Arguments,
            Rules.RequiredPair_Attributes_DataDrivenTest_Arguments,
            methodSymbol.Locations.FirstOrDefault());

        Compare(context, attributes, 
            WellKnown.AttributeFullyQualifiedClasses.EnumerableDataSourceDrivenTest,
            WellKnown.AttributeFullyQualifiedClasses.EnumerableMethodDataSource,
            Rules.RequiredPair_Attributes_EnumerableDataSourceDrivenTest_EnumerableMethodInfo,
            methodSymbol.Locations.FirstOrDefault());

        Compare(context, attributes, 
            WellKnown.AttributeFullyQualifiedClasses.DataSourceDrivenTest,
            [WellKnown.AttributeFullyQualifiedClasses.MethodDataSource, WellKnown.AttributeFullyQualifiedClasses.ClassDataSource],
            Rules.RequiredCombinations_Attributes_DataSourceDrivenTest_MethodInfo_ClassInfo,
            methodSymbol.Locations.FirstOrDefault());
    }

    private void Compare(SyntaxNodeAnalysisContext context, 
        ImmutableArray<AttributeData> attributes,
        string requiredTestAttributeName, 
        string requiredDataAttributeName,
        DiagnosticDescriptor diagnosticDescriptor,
        Location? location)
    {
        var testAttribute = attributes.Get(requiredTestAttributeName);
        var dataAttribute = attributes.Get(requiredDataAttributeName);

        if (testAttribute is null && dataAttribute is null)
        {
            return;
        }

        if (testAttribute is null || dataAttribute is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location));
        }
    }
    
    private void Compare(SyntaxNodeAnalysisContext context, 
        ImmutableArray<AttributeData> attributes,
        string requiredTestAttributeName, 
        IEnumerable<string> requiredDataAttributes,
        DiagnosticDescriptor diagnosticDescriptor,
        Location? location)
    {
        var testAttribute = attributes.Get(requiredTestAttributeName);
        var dataAttributes = requiredDataAttributes.Select(x => attributes.Get(x)).ToList();

        if (testAttribute is null && dataAttributes.All(x => x is null))
        {
            return;
        }
        
        foreach (var dataAttribute in dataAttributes.OfType<AttributeData>())
        {
            if (testAttribute is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor,
                    dataAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? location)
                );
            }
        }

        if (testAttribute is not null
            && dataAttributes.All(x => x is null))
        {
            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor,
                testAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? location)
            );
        }
    }
}