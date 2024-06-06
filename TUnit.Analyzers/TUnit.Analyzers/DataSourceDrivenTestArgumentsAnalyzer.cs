using System;
using System.Collections.Generic;
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
public class DataSourceDrivenTestArgumentsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.NoTestDataSourceProvided,
            Rules.NoDataSourceMethodFound,
            Rules.MethodMustBeStatic,
            Rules.MethodMustBePublic,
            Rules.MethodMustNotBeAbstract,
            Rules.MethodMustBeParameterless,
            Rules.WrongArgumentTypeTestDataSource,
            Rules.MethodMustReturnData,
            Rules.NoArgumentInTestMethod,
            Rules.TooManyArgumentsInTestMethod
        );

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
    }
    
    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
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

        var methodParameterTypes = methodSymbol.Parameters.IsDefaultOrEmpty
            ? []
            : methodSymbol.Parameters.ToList();
        
        if (methodSymbol.HasTimeoutAttribute(out _)
            && SymbolEqualityComparer.Default.Equals(methodParameterTypes.LastOrDefault()?.Type,
                context.Compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)))
        {
            methodParameterTypes.RemoveAt(methodParameterTypes.Count - 1);
        }

        var attributes = methodSymbol.GetAttributes();
        
        foreach (var dataSourceDrivenAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                                        == WellKnown.AttributeFullyQualifiedClasses.MethodDataSource))
        {
            CheckAttributeAgainstMethod(context, methodParameterTypes.ToImmutableArray(), dataSourceDrivenAttribute, methodSymbol.ContainingType);
        }
    }
    
    private void AnalyzeClass(SyntaxNodeAnalysisContext context)
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

        var attributes = namedTypeSymbol.GetAttributes();
        
        foreach (var dataSourceDrivenAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                                        == WellKnown.AttributeFullyQualifiedClasses.MethodDataSource))
        {
            CheckAttributeAgainstMethod(context, namedTypeSymbol.Constructors.FirstOrDefault()?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty, dataSourceDrivenAttribute, namedTypeSymbol);
        }
    }

    private void CheckAttributeAgainstMethod(SyntaxNodeAnalysisContext context, 
        ImmutableArray<IParameterSymbol> parameters,
        AttributeData dataSourceDrivenAttribute,
        INamedTypeSymbol fallbackClassToSearchForDataSourceIn)
    {
        if (parameters.Length == 0)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.NoArgumentInTestMethod,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }

        var shouldUnfoldTuple = dataSourceDrivenAttribute.NamedArguments
            .FirstOrDefault(x => x.Key == "UnfoldTuple")
            .Value
            .Value as bool? ?? false;
        
        if (parameters.Length > 1 && !shouldUnfoldTuple)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TooManyArgumentsInTestMethod,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        var methodParameterTypes = parameters.Select(x => x.Type).ToList();
        
        var methodContainingTestData = FindMethodContainingTestData(context, dataSourceDrivenAttribute, fallbackClassToSearchForDataSourceIn);
        
        if (methodContainingTestData is null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.NoDataSourceMethodFound,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.ReturnsVoid)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustReturnData,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        if (!methodContainingTestData.IsStatic)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustBeStatic,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustBePublic,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.Parameters.Any())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodMustBeParameterless,
                    dataSourceDrivenAttribute.GetLocation())
            );
            return;
        }

        var argumentType = methodContainingTestData.ReturnType;

        if (argumentType.IsTupleType)
        {
            var actualMethodParameterTypes = parameters.Select(x => x.Type).ToList();
            
            var tupleTypes = argumentType.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                .TrimStart('(')
                .TrimEnd(')')
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            if (actualMethodParameterTypes.Count != tupleTypes.Count)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rules.WrongArgumentTypeTestDataSource,
                        dataSourceDrivenAttribute.GetLocation(),
                        argumentType,
                        FormatParametersToString(methodParameterTypes))
                );
                return;
            }

            for (var index = 0; index < tupleTypes.Count; index++)
            {
                var tupleParameterType = tupleTypes[index];
                var actualParameterType = actualMethodParameterTypes[index];

                if (tupleParameterType !=
                    actualParameterType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Rules.WrongArgumentTypeTestDataSource,
                            dataSourceDrivenAttribute.GetLocation(),
                            argumentType,
                            FormatParametersToString(methodParameterTypes))
                    );
                }
            }
            return;
        }
        
        var enumerableOfMethodType = CreateEnumerableOfType(context, methodParameterTypes.ElementAt(0));

        if (context.Compilation.HasImplicitConversion(argumentType, methodParameterTypes.ElementAt(0))
            || context.Compilation.HasImplicitConversion(argumentType, enumerableOfMethodType))
        {
            return;
        }
        
        context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.WrongArgumentTypeTestDataSource,
                    dataSourceDrivenAttribute.GetLocation(),
                    argumentType,
                    FormatParametersToString(methodParameterTypes))
            );
    }

    private string FormatParametersToString(List<ITypeSymbol> methodParameterTypes)
    {
        if (!methodParameterTypes.Any())
        {
            return "()";
        }

        if (methodParameterTypes.Count == 1)
        {
            return methodParameterTypes[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }

        return
            $"({string.Join(", ", methodParameterTypes.Select(x => x.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))})";
    }

    private static ITypeSymbol CreateEnumerableOfType(SyntaxNodeAnalysisContext context, ITypeSymbol typeSymbol)
    {
        return context.SemanticModel.Compilation
            .GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
            .Construct(typeSymbol);
    }

    private IMethodSymbol? FindMethodContainingTestData(SyntaxNodeAnalysisContext context, AttributeData dataSourceDrivenAttribute,
        INamedTypeSymbol classContainingTest)
    {
        if (dataSourceDrivenAttribute.ConstructorArguments.Length == 1)
        {
            var methodName = dataSourceDrivenAttribute.ConstructorArguments.First().Value as string;
            return classContainingTest.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == methodName);
        }
        else
        {
            var methodName = dataSourceDrivenAttribute.ConstructorArguments[1].Value as string;

            var @class =
                dataSourceDrivenAttribute.ConstructorArguments[0].Value as INamedTypeSymbol ?? classContainingTest;
            
            return @class?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == methodName);
        }
    }
}