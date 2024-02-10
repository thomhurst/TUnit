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
    // Preferred format of DiagnosticId is Your Prefix + Number, e.g. CA1234.
    public const string DiagnosticId = "TUnit0003";

    // Feel free to use raw strings if you don't need localization.
    private static readonly LocalizableString Title = new LocalizableResourceString(DiagnosticId + "Title",
        Resources.ResourceManager, typeof(Resources));

    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat =
        new LocalizableResourceString(DiagnosticId + "MessageFormat", Resources.ResourceManager,
            typeof(Resources));

    private static readonly LocalizableString Description =
        new LocalizableResourceString(DiagnosticId + "Description", Resources.ResourceManager,
            typeof(Resources));

    // The category of the diagnostic (Design, Naming etc.).
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category,
        DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

    // Keep in mind: you have to list your rules here.
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

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
                    new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error,
                        true, Description),
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
                    new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error,
                        true, Description),
                    dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation())
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
                        new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error,
                            true, Description),
                        dataDrivenTestAttribute.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                        string.Join(", ", attributeTypesPassedIn.Select(x => x?.ToDisplayString())),
                        string.Join(", ", methodParameterTypes.Select(x => x?.ToDisplayString())))
                );
            }
        }
    }
}