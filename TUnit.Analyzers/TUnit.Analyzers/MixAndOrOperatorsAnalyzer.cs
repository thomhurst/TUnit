using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

/// <summary>
/// A sample analyzer that reports the company name being used in class declarations.
/// Traverses through the Syntax Tree and checks the name (identifier) of each class node.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MixAndOrOperatorsAnalyzer : DiagnosticAnalyzer
{
    public const string CompanyName = "MyCompany";

    // Preferred format of DiagnosticId is Your Prefix + Number, e.g. CA1234.
    public const string DiagnosticId = "TUnit0001";

    // Feel free to use raw strings if you don't need localization.
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.TUnit0001Title),
        Resources.ResourceManager, typeof(Resources));

    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat =
        new LocalizableResourceString(nameof(Resources.TUnit0001MessageFormat), Resources.ResourceManager,
            typeof(Resources));

    private static readonly LocalizableString Description =
        new LocalizableResourceString(nameof(Resources.TUnit0001Description), Resources.ResourceManager,
            typeof(Resources));

    // The category of the diagnostic (Design, Naming etc.).
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

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
        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SimpleMemberAccessExpression);

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
        if (context.Node is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            return;

        if (memberAccessExpressionSyntax.Name.Identifier.Value is not "And" and not "Or")
        {
            return;
        }

        if (memberAccessExpressionSyntax.Parent?.Parent is not InvocationExpressionSyntax invocationExpressionSyntax)
        {
            return;
        }
        
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax);

        var fullyQualifiedSymbolInformation = symbolInfo.Symbol?.ToDisplayString(DisplayFormats.FullyQualifiedNonGeneric) 
                                              ?? string.Empty;
        
        if (!fullyQualifiedSymbolInformation.StartsWith("global::TUnit.Assertions"))
        {
            return;
        }

        var fullInvocationStatement = invocationExpressionSyntax.ToFullString();
        if(fullInvocationStatement.Contains(".And.") 
           && fullInvocationStatement.Contains(".Or."))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
                        true, Description),
                    invocationExpressionSyntax.GetLocation())
            );
        }
    }
}