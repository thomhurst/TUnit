using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataSourceDrivenTestArgumentsAnalyzer : DiagnosticAnalyzer
{
    public const string MismatchedArgumentsDiagnosticId = "TUnit0005";
    
    private static readonly LocalizableString MismatchedArgumentsTitle = new LocalizableResourceString(MismatchedArgumentsDiagnosticId + "Title",
        Resources.ResourceManager, typeof(Resources));
    
    private static readonly LocalizableString MismatchedArgumentsMessageFormat =
        new LocalizableResourceString(MismatchedArgumentsDiagnosticId + "MessageFormat", Resources.ResourceManager,
            typeof(Resources));

    private static readonly LocalizableString MismatchedArgumentsDescription =
        new LocalizableResourceString(MismatchedArgumentsDiagnosticId + "Description", Resources.ResourceManager,
            typeof(Resources));
    
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor MismatchedArgumentsRule = new(MismatchedArgumentsDiagnosticId, MismatchedArgumentsTitle, MismatchedArgumentsMessageFormat, Category,
        DiagnosticSeverity.Error, isEnabledByDefault: true, description: MismatchedArgumentsDescription);

    public const string NotFoundDataSourceDiagnosticId = "TUnit0006";
    
    private static readonly LocalizableString NotFoundDataSourceTitle = new LocalizableResourceString(MismatchedArgumentsDiagnosticId + "Title",
        Resources.ResourceManager, typeof(Resources));
    
    private static readonly LocalizableString NotFoundDataSourceMessageFormat =
        new LocalizableResourceString(NotFoundDataSourceDiagnosticId + "MessageFormat", Resources.ResourceManager,
            typeof(Resources));

    private static readonly LocalizableString NotFoundDataSourceDescription =
        new LocalizableResourceString(NotFoundDataSourceDiagnosticId + "Description", Resources.ResourceManager,
            typeof(Resources));
    
    private static readonly DiagnosticDescriptor NotFoundDataSourceRule = new(NotFoundDataSourceDiagnosticId, NotFoundDataSourceTitle, NotFoundDataSourceMessageFormat, Category,
        DiagnosticSeverity.Error, isEnabledByDefault: true, description: NotFoundDataSourceDescription);
    
    // Keep in mind: you have to list your rules here.
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(MismatchedArgumentsRule, NotFoundDataSourceRule);

    public override void Initialize(AnalysisContext context)
    {
        // You must call this method to avoid analyzing generated code.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // You must call this method to enable the Concurrent Execution.
        context.EnableConcurrentExecution();

        // Subscribe to the Syntax Node with the appropriate 'SyntaxKind' (ClassDeclaration) action.
        // To figure out which Syntax Nodes you should choose, consider installing the Roslyn syntax tree viewer plugin Rossynt: https://plugins.jetbrains.com/plugin/16902-rossynt/
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.MethodDeclaration);

        // Check other 'context.Register...' methods that might be helpful for your purposes.
    }

    /// <summary>
    /// Executed for each Syntax Node with 'SyntaxKind' is 'ClassDeclaration'.
    /// </summary>
    /// <param name="context">Operation context.</param>
    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        // The Roslyn architecture is based on inheritance.
        // To get the required metadata, we should match the 'Node' object to the particular type: 'ClassDeclarationSyntax'.
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
        
        foreach (var dataDrivenTestAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGeneric)
                                                == "global::TUnit.Core.DataSourceDrivenTestAttribute"))
        {
            CheckAttributeAgainstMethod(context, methodSymbol, dataDrivenTestAttribute);
        }
    }

    private void CheckAttributeAgainstMethod(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol,
        AttributeData dataDrivenTestAttribute)
    {
        var methodParameterType = methodSymbol.Parameters.Select(x => x.Type).FirstOrDefault();
        var methodContainingTestData = FindMethodContainingTestData(context, dataDrivenTestAttribute, methodSymbol.ContainingType);

        if (methodContainingTestData is null 
            || methodContainingTestData.Parameters.Any()
            || methodContainingTestData.ReturnsVoid)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(NotFoundDataSourceDiagnosticId, NotFoundDataSourceTitle,
                        NotFoundDataSourceMessageFormat, Category, DiagnosticSeverity.Error,
                        true, NotFoundDataSourceDescription),
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }

        var argumentType = methodContainingTestData.ReturnType;

        var enumerableOfMethodType = CreateEnumerableOfType(context, methodParameterType!);
        
        if (!SymbolEqualityComparer.Default.Equals(argumentType, methodParameterType)
            && !SymbolEqualityComparer.Default.Equals(argumentType, enumerableOfMethodType))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(MismatchedArgumentsDiagnosticId, MismatchedArgumentsTitle, MismatchedArgumentsMessageFormat, Category, DiagnosticSeverity.Error,
                        true, MismatchedArgumentsDescription),
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    argumentType,
                    methodParameterType)
            );
        }
    }

    private static ITypeSymbol CreateEnumerableOfType(SyntaxNodeAnalysisContext context, ITypeSymbol typeSymbol)
    {
        var fullyQualifiedFormat = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var type = Type.GetType(fullyQualifiedFormat);

        var enumerableType = typeof(IEnumerable<>).MakeGenericType(type);

        return context.Compilation.GetTypeByMetadataName(enumerableType.FullName!)!;
    }

    private IMethodSymbol? FindMethodContainingTestData(SyntaxNodeAnalysisContext context, AttributeData dataDrivenTestAttribute,
        INamedTypeSymbol classContainingTest)
    {
        if (dataDrivenTestAttribute.ConstructorArguments.Length == 1)
        {
            var methodName = dataDrivenTestAttribute.ConstructorArguments.First().Value as string;
            return classContainingTest.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == methodName);
        }
        else
        {
            var methodName = dataDrivenTestAttribute.ConstructorArguments[1].Value as string;

            var @class =
                dataDrivenTestAttribute.ConstructorArguments[0].Value is not Type classType
                    ? classContainingTest
                    : context.Compilation.GetTypeByMetadataName(classType.AssemblyQualifiedName ?? classType.FullName!);
            
            return @class?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == methodName);
        }
    }
}