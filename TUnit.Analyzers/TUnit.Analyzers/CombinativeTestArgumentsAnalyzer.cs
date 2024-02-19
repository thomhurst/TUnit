using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CombinativeTestArgumentsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.WrongArgumentTypeTestData, Rules.NoTestDataProvided, Rules.MethodParameterBadNullability);

    public override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.Parameter);
    }
    
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    { 
        if (context.Node is not ParameterSyntax parameterSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(parameterSyntax)
            is not { } parameterSymbol)
        {
            return;
        }

        var combinativeValuesAttribute = parameterSymbol.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                        == "global::TUnit.Core.CombinativeValuesAttribute");

        if (combinativeValuesAttribute is null)
        {
            return;
        }

        var objectParamsArray = combinativeValuesAttribute.ConstructorArguments;

        if (objectParamsArray.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoTestDataProvided,
                    combinativeValuesAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        foreach (var typedConstant in objectParamsArray[0].Values)
        {
            if (!IsEnumAndInteger(typedConstant.Type, parameterSymbol.Type)
                && !context.Compilation.HasImplicitConversion(typedConstant.Type, parameterSymbol.Type))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                        combinativeValuesAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                        typedConstant.Type?.ToDisplayString(),
                        parameterSymbol.ToDisplayString())
                );
            }
        }
    }

    private bool IsEnumAndInteger(ITypeSymbol? type1, ITypeSymbol? type2)
    {
        if (type1?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "int")
        {
            return type2?.TypeKind == TypeKind.Enum;
        }
        
        if (type2?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "int")
        {
            return type1?.TypeKind == TypeKind.Enum;
        }

        return false;
    }
}