using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TUnit.Analyzers;

/// <summary>
/// A sample analyzer that reports invalid values being used for the 'speed' parameter of the 'SetSpeed' function.
/// To make sure that we analyze the method of the specific class, we use semantic analysis instead of the syntax tree, so this analyzer will not work if the project is not compilable.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SampleSemanticAnalyzer : DiagnosticAnalyzer
{
    private const string CommonApiClassName = "Spaceship";
    private const string CommonApiMethodName = "SetSpeed";

    // Preferred format of DiagnosticId is Your Prefix + Number, e.g. CA1234.
    private const string DiagnosticId = "TUnit0002";

    // Feel free to use raw strings if you don't need localization.
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.TUnit0002Title),
        Resources.ResourceManager, typeof(Resources));

    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat =
        new LocalizableResourceString(nameof(Resources.TUnit0002MessageFormat), Resources.ResourceManager,
            typeof(Resources));

    private static readonly LocalizableString Description =
        new LocalizableResourceString(nameof(Resources.TUnit0002Description), Resources.ResourceManager,
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

        // Subscribe to semantic (compile time) action invocation, e.g. method invocation.
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);

        // Check other 'context.Register...' methods that might be helpful for your purposes.
    }

    /// <summary>
    /// Executed on the completion of the semantic analysis associated with the Invocation operation.
    /// </summary>
    /// <param name="context">Operation context.</param>
    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        // The Roslyn architecture is based on inheritance.
        // To get the required metadata, we should match the 'Operation' and 'Syntax' objects to the particular types,
        // which are based on the 'OperationKind' parameter specified in the 'Register...' method.
        if (context.Operation is not IInvocationOperation invocationOperation ||
            context.Operation.Syntax is not InvocationExpressionSyntax invocationSyntax)
            return;

        var methodSymbol = invocationOperation.TargetMethod;

        // Check whether the method name is 'SetSpeed' and it is a member of the 'Spaceship' class.
        if (methodSymbol.MethodKind != MethodKind.Ordinary ||
            methodSymbol.ReceiverType?.Name != CommonApiClassName ||
            methodSymbol.Name != CommonApiMethodName
           )
            return;

        // Count validation is enough in most cases. Keep analyzers as simple as possible.
        if (invocationSyntax.ArgumentList.Arguments.Count != 1)
            return;

        // Traverse through the syntax tree, starting with the particular 'InvocationSyntax' to the desired node.
        var argumentSyntax = invocationSyntax.ArgumentList.Arguments.Single().Expression;

        // The 'ToString' method of 'Syntax' classes returns the corresponding part of the source code.
        var argument = argumentSyntax.ToString();

        if (!int.TryParse(argument, out var actualSpeed))
            return;

        if (actualSpeed <= 299_792_458)
            return;

        var diagnostic = Diagnostic.Create(Rule,
            // The highlighted area in the analyzed source code. Keep it as specific as possible.
            argumentSyntax.GetLocation(),
            // The value is passed to the 'MessageFormat' argument of your rule.
            actualSpeed);

        // Reporting a diagnostic is the primary outcome of analyzers.
        context.ReportDiagnostic(diagnostic);
    }
}