using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.EqualityComparers;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GeneratedDataAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create
        (
            Rules.TypeMustBePublic,
            Rules.Argument_Count_Not_Matching_Parameter_Count,
            Rules.WrongArgumentTypeTestDataSource,
            Rules.NoMatchingParameterClassDataSource,
            Rules.ConstructorMustBeParameterless
        );

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Property);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        List<ITypeSymbol> parameterOrPropertyTypes = [];

        List<AttributeData> attributes = [];
        if (context.Symbol is IMethodSymbol methodSymbol)
        {
            parameterOrPropertyTypes.AddRange(methodSymbol.Parameters.Select(x => x.Type));
            attributes.AddRange(methodSymbol.GetAttributes());
        }

        if (context.Symbol is INamedTypeSymbol namedTypeSymbol)
        {
            parameterOrPropertyTypes.AddRange(namedTypeSymbol.InstanceConstructors.FirstOrDefault()?.Parameters
                .Select(x => x.Type) ?? []);
            attributes.AddRange(namedTypeSymbol.GetAttributes());
        }

        if (context.Symbol is IPropertySymbol propertySymbol)
        {
            parameterOrPropertyTypes.Add(propertySymbol.Type);
            attributes.AddRange(propertySymbol.GetAttributes());
        }

        foreach (var attributeData in attributes)
        {
            var selfAndBaseTypes = attributeData.AttributeClass?.GetSelfAndBaseTypes() ?? [];
            
            var baseGeneratorAttribute = selfAndBaseTypes
                .FirstOrDefault(x => x.Interfaces.Any(i => i.GloballyQualified() == WellKnown.AttributeFullyQualifiedClasses.IDataSourceGeneratorAttribute));

            if (baseGeneratorAttribute is null)
            {
                continue;
            }

            if (baseGeneratorAttribute.TypeArguments.SequenceEqual(parameterOrPropertyTypes, SelfOrBaseEqualityComparer.Instance))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rules.WrongArgumentTypeTestDataSource,
                attributeData.GetLocation() ?? context.Symbol.Locations.FirstOrDefault(),
                string.Join(", ", baseGeneratorAttribute.TypeArguments), string.Join(", ", parameterOrPropertyTypes)));
        }
    }
}