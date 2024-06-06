using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataDrivenTestArgumentsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.WrongArgumentTypeTestData, Rules.NoTestDataProvided, Rules.MethodParameterBadNullability, Rules.MissingDataDrivenTestAttribute);

    protected override void InitializeInternal(AnalysisContext context)
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

        if (methodSymbol.GetDataDrivenTestAttribute() != null
            && methodSymbol.GetArgumentsAttribute() == null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoTestDataProvided,
                    methodDeclarationSyntax.GetLocation())
            );
            return;
        }

        var attributes = methodSymbol.GetAttributes();
        
        foreach (var argumentsAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                == WellKnown.AttributeFullyQualifiedClasses.Arguments))
        {
            CheckAttributeAgainstMethod(context, methodSymbol, argumentsAttribute);
        }
    }

    private void CheckAttributeAgainstMethod(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol,
        AttributeData argumentsAttribute)
    {
        if (methodSymbol.GetDataDrivenTestAttribute() == null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.MissingDataDrivenTestAttribute,
                    argumentsAttribute.GetLocation() ?? methodSymbol.Locations.FirstOrDefault())
            );
        }
        
        if (argumentsAttribute.ConstructorArguments.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoTestDataProvided,
                    argumentsAttribute.GetLocation() ?? methodSymbol.Locations.FirstOrDefault())
            );
            return;
        }
        
        var methodParameterTypes = methodSymbol.Parameters.Select(x => x.Type).ToList();
        var objectArrayArgument = argumentsAttribute.ConstructorArguments.First();
        var attributeTypesPassedIn = 
            objectArrayArgument.IsNull ? [null] : 
            objectArrayArgument.Values.Select(x => x.IsNull ? null : x.Type).ToList();

        if (methodSymbol.HasTimeoutAttribute(out _)
            && SymbolEqualityComparer.Default.Equals(methodParameterTypes.LastOrDefault(),
                context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)))
        {
            methodParameterTypes.RemoveAt(methodParameterTypes.Count - 1);
        }
        
        if (methodParameterTypes.Count != attributeTypesPassedIn.Count)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                    argumentsAttribute.GetLocation(),
                    string.Join(", ", attributeTypesPassedIn.Select(x => x?.ToDisplayString()) ?? ImmutableArray<string>.Empty),
                    string.Join(", ", methodParameterTypes.Select(x => x?.ToDisplayString())))
            );
            return;
        }
        
        for (var i = 0; i < methodParameterTypes.Count; i++)
        {
            var methodParameterType = methodParameterTypes[i];
            var attributeArgumentType = attributeTypesPassedIn[i];
            
            if (attributeArgumentType is null &&
                methodParameterType.NullableAnnotation == NullableAnnotation.NotAnnotated)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.MethodParameterBadNullability,
                        methodSymbol.Parameters[i].Locations.FirstOrDefault(),
                        methodSymbol.Parameters[i].Name)
                );
            }
            
            if (IsEnumAndInteger(methodParameterType, attributeArgumentType))
            {
                continue;
            }
            
            if (attributeArgumentType is not null &&
                !context.Compilation.HasImplicitConversion(attributeArgumentType, methodParameterType))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                        argumentsAttribute.GetLocation() ?? methodSymbol.Locations.FirstOrDefault(),
                        attributeArgumentType.ToDisplayString(),
                        methodParameterType.ToDisplayString())
                );
                return;
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