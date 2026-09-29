using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Assertions.Analyzers.Extensions;
using TUnit.Assertions.Analyzers.Helpers;

namespace TUnit.Assertions.Analyzers;

/// <summary>
/// Reports explicitly passed arguments for TUnit.Assertions parameters that the compiler is meant to populate
/// (<c>[CallerMemberName]</c> / <c>[CallerArgumentExpression]</c>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CompilerArgumentsPopulatedAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.CompilerArgumentsPopulated);

    public override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationStart =>
        {
            // Every assembly defining TUnit.Assertions.Assert: a global reference plus any extern-aliased copies.
            var assertionsAssemblies = AssertionSymbols.For(compilationStart.Compilation).Assert;

            if (assertionsAssemblies.IsEmpty)
            {
                return;
            }

            // Caller-info flags per parameter, per method definition. Almost every TUnit assertion method
            // has [CallerArgumentExpression] parameters, so decoding their attributes once pays off.
            var callerInfoParameters = new ConcurrentDictionary<IMethodSymbol, bool[]>(SymbolEqualityComparer.Default);

            // Registering on invocations/object creations (rather than every argument in the compilation)
            // lets calls to methods outside TUnit.Assertions be rejected once, not once per argument.
            compilationStart.RegisterOperationAction(ctx =>
            {
                var invocation = (IInvocationOperation)ctx.Operation;
                AnalyzeArguments(ctx, invocation.TargetMethod, invocation.Arguments, assertionsAssemblies, callerInfoParameters);
            }, OperationKind.Invocation);

            compilationStart.RegisterOperationAction(ctx =>
            {
                var objectCreation = (IObjectCreationOperation)ctx.Operation;
                AnalyzeArguments(ctx, objectCreation.Constructor, objectCreation.Arguments, assertionsAssemblies, callerInfoParameters);
            }, OperationKind.ObjectCreation);
        });
    }

    private static void AnalyzeArguments(
        OperationAnalysisContext context,
        IMethodSymbol? method,
        ImmutableArray<IArgumentOperation> arguments,
        TypeSymbolSet assertionsAssemblies,
        ConcurrentDictionary<IMethodSymbol, bool[]> callerInfoParameters)
    {
        if (method is null
            || arguments.IsEmpty
            || !assertionsAssemblies.ContainsAssembly(method.ContainingAssembly))
        {
            return;
        }

        foreach (var argumentOperation in arguments)
        {
            // Only arguments written in source: implicit arguments are the compiler-populated defaults.
            if (argumentOperation.IsImplicit
                || argumentOperation.Syntax is not ArgumentSyntax argumentSyntax
                || argumentOperation.Parameter is not { ContainingSymbol: IMethodSymbol parameterOwner } parameter)
            {
                continue;
            }

            var flags = callerInfoParameters.GetOrAdd(parameterOwner.OriginalDefinition, static m => GetCallerInfoFlags(m));

            if (parameter.Ordinal < flags.Length && flags[parameter.Ordinal])
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rules.CompilerArgumentsPopulated,
                        argumentSyntax.GetLocation())
                );
            }
        }
    }

    private static bool[] GetCallerInfoFlags(IMethodSymbol method)
    {
        var parameters = method.Parameters;
        var flags = new bool[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            foreach (var attribute in parameters[i].GetAttributes())
            {
                if (attribute.AttributeClass is { } attributeClass
                    && (attributeClass.IsGloballyQualified("global::System.Runtime.CompilerServices.CallerMemberNameAttribute")
                        || attributeClass.IsGloballyQualified("global::System.Runtime.CompilerServices.CallerArgumentExpressionAttribute")))
                {
                    flags[i] = true;
                    break;
                }
            }
        }

        return flags;
    }
}
