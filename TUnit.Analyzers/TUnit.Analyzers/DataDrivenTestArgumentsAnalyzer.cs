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
public class DataDrivenTestArgumentsAnalyzer : DiagnosticAnalyzer
{
    public const string MismatchedArgumentsDiagnosticId = "TUnit0003";
    
    private static readonly LocalizableString MismatchedArgumentsTitle = new LocalizableResourceString(MismatchedArgumentsDiagnosticId + "Title",
        Resources.ResourceManager, typeof(Resources));
    
    private static readonly LocalizableString MismatchedArgumentsMessageFormat =
        new LocalizableResourceString(MismatchedArgumentsDiagnosticId + "MessageFormat", Resources.ResourceManager,
            typeof(Resources));

    private static readonly LocalizableString MismatchedArgumentsDescription =
        new LocalizableResourceString(MismatchedArgumentsDiagnosticId + "Description", Resources.ResourceManager,
            typeof(Resources));
    
    public const string MissingArgumentsDiagnosticId = "TUnit0004";
    
    private static readonly LocalizableString MissingArgumentsTitle = new LocalizableResourceString(MismatchedArgumentsDiagnosticId + "Title",
        Resources.ResourceManager, typeof(Resources));
    
    private static readonly LocalizableString MissingArgumentsMessageFormat =
        new LocalizableResourceString(MissingArgumentsDiagnosticId + "MessageFormat", Resources.ResourceManager,
            typeof(Resources));

    private static readonly LocalizableString MissingArgumentsDescription =
        new LocalizableResourceString(MissingArgumentsDiagnosticId + "Description", Resources.ResourceManager,
            typeof(Resources));

    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor MismatchedArgumentsRule = new(MismatchedArgumentsDiagnosticId, MismatchedArgumentsTitle, MismatchedArgumentsMessageFormat, Category,
        DiagnosticSeverity.Error, isEnabledByDefault: true, description: MismatchedArgumentsDescription);
    
    private static readonly DiagnosticDescriptor MissingArgumentsRule = new(MissingArgumentsDiagnosticId, MissingArgumentsTitle, MissingArgumentsMessageFormat, Category,
        DiagnosticSeverity.Error, isEnabledByDefault: true, description: MissingArgumentsDescription);

    // Keep in mind: you have to list your rules here.
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(MismatchedArgumentsRule, MissingArgumentsRule);

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
                                                == "global::TUnit.Core.DataDrivenTestAttribute"))
        {
            CheckAttributeAgainstMethod(context, methodSymbol, dataDrivenTestAttribute);
        }
    }

    private void CheckAttributeAgainstMethod(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol,
        AttributeData dataDrivenTestAttribute)
    {
        if (!dataDrivenTestAttribute.ConstructorArguments.Any())
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(MissingArgumentsDiagnosticId, MissingArgumentsTitle, MissingArgumentsMessageFormat, Category, DiagnosticSeverity.Error,
                        true, MissingArgumentsDescription),
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
            );
            return;
        }

        var methodParameterTypes = methodSymbol.Parameters.Select(x => x.Type).ToList();
        var attributeTypesPassedIn = dataDrivenTestAttribute.ConstructorArguments.First().Values.Select(x => x.Type).ToList();

        if (methodParameterTypes.Count != attributeTypesPassedIn.Count)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(MismatchedArgumentsDiagnosticId, MismatchedArgumentsTitle, MismatchedArgumentsMessageFormat, Category, DiagnosticSeverity.Error,
                        true, MismatchedArgumentsDescription),
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    string.Join(", ", attributeTypesPassedIn.Select(x => x?.ToDisplayString())),
                    string.Join(", ", methodParameterTypes.Select(x => x?.ToDisplayString())))
            );
            return;
        }
        
        for (var i = 0; i < methodParameterTypes.Count; i++)
        {
            var methodParameterType = methodParameterTypes[i];
            var attributeArgumentType = attributeTypesPassedIn[i];
            
            if (!context.Compilation.HasImplicitConversion(attributeArgumentType, methodParameterType))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(MismatchedArgumentsDiagnosticId, MismatchedArgumentsTitle, MismatchedArgumentsMessageFormat, Category, DiagnosticSeverity.Error,
                            true, MismatchedArgumentsDescription),
                        dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                        attributeArgumentType?.ToDisplayString(),
                        methodParameterType?.ToDisplayString())
                );
            }
        }
    }
}