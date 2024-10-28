using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MatrixTestArgumentsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.WrongArgumentTypeTestData, Rules.NoTestDataProvided, Rules.MethodParameterBadNullability);

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Parameter);
    }
    
    private void AnalyzeSymbol(SymbolAnalysisContext context)
    { 
        if (context.Symbol is not IParameterSymbol parameterSymbol)
        {
            return;
        }

        var matrixAttribute = parameterSymbol.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                        == WellKnown.AttributeFullyQualifiedClasses.Matrix.WithGlobalPrefix);

        if (matrixAttribute is null)
        {
            return;
        }

        var objectParamsArray = matrixAttribute.ConstructorArguments;

        if (objectParamsArray.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoTestDataProvided,
                    matrixAttribute.GetLocation())
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
                        matrixAttribute.GetLocation(),
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