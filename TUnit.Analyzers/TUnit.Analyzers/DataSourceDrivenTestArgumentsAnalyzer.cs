using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataSourceDrivenTestArgumentsAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.NoTestDataSourceProvided,
            Rules.NoDataSourceMethodFound,
            Rules.MethodNotStatic,
            Rules.MethodNotPublic,
            Rules.MethodAbstract,
            Rules.MethodNotParameterless,
            Rules.WrongArgumentTypeTestDataSource,
            Rules.MethodNotReturnsNothing,
            Rules.NoArgumentInTestMethod,
            Rules.TooManyArgumentsInTestMethod
        );

    public override void InitializeInternal(AnalysisContext context)
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

        var attributes = methodSymbol.GetAttributes();
        
        foreach (var dataSourceDrivenAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                                        == "global::TUnit.Core.DataSourceDrivenTestAttribute"))
        {
            CheckAttributeAgainstMethod(context, methodSymbol.Parameters, dataSourceDrivenAttribute, methodSymbol.ContainingType);
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
                                                                        == "global::TUnit.Core.DataSourceDrivenTestAttribute"))
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
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        if (parameters.Length > 1)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TooManyArgumentsInTestMethod,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        var methodParameterType = parameters.Select(x => x.Type).FirstOrDefault();
        
        var methodContainingTestData = FindMethodContainingTestData(context, dataSourceDrivenAttribute, fallbackClassToSearchForDataSourceIn);
        
        if (methodContainingTestData is null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.NoDataSourceMethodFound,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.ReturnsVoid)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodNotReturnsNothing,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        if (!methodContainingTestData.IsStatic)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodNotStatic,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodNotPublic,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }
        
        if (methodContainingTestData.Parameters.Any())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.MethodNotParameterless,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }

        var argumentType = methodContainingTestData.ReturnType;

        var enumerableOfMethodType = CreateEnumerableOfType(context, methodParameterType!);

        if (context.Compilation.HasImplicitConversion(argumentType, methodParameterType)
            || context.Compilation.HasImplicitConversion(argumentType, enumerableOfMethodType))
        {
            return;
        }
        
        context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.WrongArgumentTypeTestDataSource,
                    dataSourceDrivenAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    argumentType,
                    methodParameterType)
            );
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