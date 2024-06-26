﻿using System.Collections.Generic;
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
public class ClassDataSourceMatchesConstructorAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.NoDataSourceMethodFound, Rules.Argument_Count_Not_Matching_Parameter_Count, Rules.WrongArgumentTypeTestDataSource, Rules.NotIEnumerable);

    protected override void InitializeInternal(AnalysisContext context)
    { 
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ClassDeclaration);
    }
    
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    { 
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax)
            is not { } namedTypeSymbol)
        {
            return;
        }

        var constructor = namedTypeSymbol.InstanceConstructors.FirstOrDefault();
        var parameters = constructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
        
        foreach (var attributeData in namedTypeSymbol.GetAttributes())
        {
            Check(context, namedTypeSymbol, attributeData, parameters);
        }
    }

    private void Check(SyntaxNodeAnalysisContext context, INamedTypeSymbol namedTypeSymbol, AttributeData attributeData,
        ImmutableArray<IParameterSymbol> parameters)
    {
        var attributeClass = attributeData.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
        switch (attributeClass)
        {
            case WellKnown.AttributeFullyQualifiedClasses.MethodDataSource:
            {
                var hasSpecifiedClass = attributeData.ConstructorArguments.Length > 1;

                var methodClass = hasSpecifiedClass ? attributeData.ConstructorArguments[0].Value as INamedTypeSymbol ?? namedTypeSymbol : namedTypeSymbol;
                var methodName = attributeData.ConstructorArguments[hasSpecifiedClass ? 1 : 0].Value as string ?? string.Empty;

                var dataSourceMethod = methodClass
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.IsStatic)
                    .Where(m => !m.ReturnsVoid)
                    .Where(m => m.Parameters.IsDefaultOrEmpty)
                    .Where(m => m.DeclaredAccessibility == Accessibility.Public)
                    .FirstOrDefault(m => m.Name == methodName);

                if (dataSourceMethod is null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rules.NoDataSourceMethodFound, attributeData.GetLocation())
                    );
                    return;
                }

                var returnTypes = GetReturnTypes(dataSourceMethod.ReturnType);

                if (returnTypes.Length != parameters.Length)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rules.Argument_Count_Not_Matching_Parameter_Count,
                            attributeData.GetLocation(),
                            returnTypes.Length, parameters.Length)
                    );
                    return;
                }

                for (int i = 0; i < returnTypes.Length; i++)
                {
                    var parameterType = parameters[i].Type;
                    var argumentType = returnTypes[i];

                    if (!SymbolEqualityComparer.Default.Equals(parameterType, argumentType))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(Rules.WrongArgumentTypeTestDataSource,
                                attributeData.GetLocation(),
                                argumentType, parameterType)
                        );
                        return;
                    }
                }
                
                break;
            }
            case WellKnown.AttributeFullyQualifiedClasses.ClassDataSource:
            {
                var type = attributeData.AttributeClass?.TypeArguments.ElementAtOrDefault(0) ?? (INamedTypeSymbol)attributeData.ConstructorArguments.First().Value!;

                if (parameters.Length != 1 || !SymbolEqualityComparer.Default.Equals(type, parameters.FirstOrDefault()?.Type))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rules.WrongArgumentTypeTestDataSource,
                            attributeData.GetLocation() ?? namedTypeSymbol.Locations.FirstOrDefault())
                    );
                }

                break;
            }
            case WellKnown.AttributeFullyQualifiedClasses.EnumerableMethodDataSource:
            {
                var hasSpecifiedClass = attributeData.ConstructorArguments.Length > 1;

                var methodClass = hasSpecifiedClass ? (INamedTypeSymbol)attributeData.ConstructorArguments[0].Value! : namedTypeSymbol;
                var methodName = (string)attributeData.ConstructorArguments[hasSpecifiedClass ? 1 : 0].Value!;

                var method = methodClass
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.IsStatic)
                    .Where(m => !m.ReturnsVoid)
                    .Where(m => m.Parameters.IsDefaultOrEmpty)
                    .Where(m => m.DeclaredAccessibility == Accessibility.Public)
                    .FirstOrDefault(m => m.Name == methodName);

                if (method is null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rules.NoDataSourceMethodFound, attributeData.GetLocation())
                    );
                    return;
                }

                var enumerableReturnType = method.ReturnType;

                if (!enumerableReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)
                    .StartsWith($"global::{typeof(IEnumerable<>).GetFullNameWithoutGenericArity()}"))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rules.NotIEnumerable, attributeData.GetLocation(), enumerableReturnType)
                    );
                    return;
                }

                var innerTypes = GetReturnTypes(((INamedTypeSymbol)enumerableReturnType).TypeArguments.First());

                if (innerTypes.Length != parameters.Length)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rules.Argument_Count_Not_Matching_Parameter_Count,
                            attributeData.GetLocation(),
                            innerTypes.Length, parameters.Length)
                    );
                    return;
                }

                for (int i = 0; i < innerTypes.Length; i++)
                {
                    var parameterType = parameters[i].Type;
                    var argumentType = innerTypes[i];

                    if (!SymbolEqualityComparer.Default.Equals(parameterType, argumentType))
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(Rules.WrongArgumentTypeTestDataSource,
                                attributeData.GetLocation(),
                                argumentType, parameterType)
                        );
                        return;
                    }
                }
                
                break;
            }
        }
    }

    private ImmutableArray<ITypeSymbol> GetReturnTypes(ITypeSymbol methodReturnType)
    {
        if (!methodReturnType.IsTupleType)
        {
            return ImmutableArray.Create(methodReturnType);
        }

        var namedTypeSymbol = (INamedTypeSymbol)methodReturnType;
        
        return namedTypeSymbol.TupleUnderlyingType?.TypeArguments
               ?? namedTypeSymbol.TypeArguments;
    }
}