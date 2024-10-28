using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataDrivenTestArgumentsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.WrongArgumentTypeTestData, Rules.NoTestDataProvided, Rules.MethodParameterBadNullability);

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
    }
    
    private void AnalyzeSymbol(SymbolAnalysisContext context)
    { 
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (methodSymbol.IsAbstract)
        {
            return;
        }

        var attributes = methodSymbol.GetAttributes();
        
        foreach (var argumentsAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                == WellKnown.AttributeFullyQualifiedClasses.Arguments.WithGlobalPrefix))
        {
            CheckAttributeAgainstMethod(context, methodSymbol, argumentsAttribute);
        }
    }

    private void CheckAttributeAgainstMethod(SymbolAnalysisContext context, IMethodSymbol methodSymbol,
        AttributeData argumentsAttribute)
    {
        if (argumentsAttribute.ConstructorArguments.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoTestDataProvided,
                    argumentsAttribute.GetLocation() ?? methodSymbol.Locations.FirstOrDefault())
            );
            return;
        }

        var parameters = methodSymbol.Parameters;
        var arguments = argumentsAttribute.ConstructorArguments.First().IsNull
            ? ImmutableArray.Create(default(TypedConstant))
            : argumentsAttribute.ConstructorArguments.First().Values;

        var cancellationTokenType = context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!);
        
        for (var i = 0; i < parameters.Length; i++)
        {
            var methodParameter = parameters[i];
            var argumentExists = i + 1 <= arguments.Length;
            var methodParameterType = methodParameter.Type;
            var argument = arguments.ElementAtOrDefault(i);
            
            if (methodSymbol.HasTimeoutAttribute(out _)
                && SymbolEqualityComparer.Default.Equals(methodParameterType, cancellationTokenType))
            {
                continue;
            }

            if (!argumentExists && methodParameter.IsOptional)
            {
                continue;
            }

            if (!argumentExists)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                        argumentsAttribute.GetLocation() ?? methodSymbol.Locations.FirstOrDefault(),
                        "null",
                        methodParameterType?.ToDisplayString())
                );
                return;
            }
            
            if (argument.IsNull && methodParameterType?.NullableAnnotation == NullableAnnotation.NotAnnotated)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.MethodParameterBadNullability,
                        parameters[i].Locations.FirstOrDefault(),
                        parameters[i].Name)
                );
            }
            
            if (IsEnumAndInteger(methodParameterType, argument.Type))
            {
                continue;
            }
            
            if (!argument.IsNull && !CanConvert(context, argument, methodParameterType))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                        argumentsAttribute.GetLocation() ?? methodSymbol.Locations.FirstOrDefault(),
                        argument.Type?.ToDisplayString(),
                        methodParameterType?.ToDisplayString())
                );
                return;
            }
        }
    }

    private static bool CanConvert(SymbolAnalysisContext context, TypedConstant argument, ITypeSymbol? methodParameterType)
    {
        if (methodParameterType?.SpecialType == SpecialType.System_Decimal &&
            argument.Type?.SpecialType == SpecialType.System_Double &&
            decimal.TryParse(argument.Value?.ToString(), out _))
        {
            // Decimals can't technically be used in attributes, but we can still write it as a double
            // e.g. [Arguments(1.55)]
            return true;
        }
        
        return context.Compilation.HasImplicitConversion(argument.Type, methodParameterType);
    }

    private bool IsEnumAndInteger(ITypeSymbol? type1, ITypeSymbol? type2)
    {
        if (type1?.SpecialType is SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64)
        {
            return type2?.TypeKind == TypeKind.Enum;
        }
        
        if (type2?.SpecialType is SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64)
        {
            return type1?.TypeKind == TypeKind.Enum;
        }

        return false;
    }
}