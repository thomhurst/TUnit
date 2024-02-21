using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataDrivenTestArgumentsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.WrongArgumentTypeTestData, Rules.NoTestDataProvided, Rules.MethodParameterBadNullability);

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
        
        foreach (var dataDrivenTestAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                == WellKnown.AttributeFullyQualifiedClasses.DataDrivenTest))
        {
            CheckAttributeAgainstMethod(context, methodSymbol, dataDrivenTestAttribute);
        }
    }

    private void CheckAttributeAgainstMethod(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol,
        AttributeData dataDrivenTestAttribute)
    {
        if (dataDrivenTestAttribute.ConstructorArguments.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.NoTestDataProvided,
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        var methodParameterTypes = methodSymbol.Parameters.Select(x => x.Type).ToList();
        var objectArrayArgument = dataDrivenTestAttribute.ConstructorArguments.First();
        var attributeTypesPassedIn = 
            objectArrayArgument.IsNull ? [null] : 
            objectArrayArgument.Values.Select(x => x.IsNull ? null : x.Type).ToList();
        
        if (methodParameterTypes.Count != attributeTypesPassedIn.Count)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Rules.WrongArgumentTypeTestData,
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
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
                        dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
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