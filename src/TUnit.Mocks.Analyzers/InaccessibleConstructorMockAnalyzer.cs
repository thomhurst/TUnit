using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Mocks.Analyzers;

/// <summary>
/// Reports TM006 when a class is mocked but declares no constructor a generated subclass could
/// chain to. The generator skips code generation for such types, so without this diagnostic the
/// only feedback would be a missing member at the call site. See issue #6493.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InaccessibleConstructorMockAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.TM006_CannotMockTypeWithoutAccessibleConstructor);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        var target = ResolveMockTarget(context, invocation);

        if (target is not { TypeKind: TypeKind.Class } namedType)
        {
            return;
        }

        // Sealed types and value types are already covered by TM001/TM002 — don't double-report.
        if (namedType.IsSealed || namedType.IsValueType)
        {
            return;
        }

        if (HasAccessibleConstructor(namedType, context.Compilation.Assembly))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rules.TM006_CannotMockTypeWithoutAccessibleConstructor,
                invocation.GetLocation(),
                namedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            )
        );
    }

    /// <summary>
    /// Resolves the mocked type from either entry point: the generic <c>Mock.Of&lt;T&gt;()</c> /
    /// <c>Mock.Wrap&lt;T&gt;()</c> form, or the generated <c>T.Mock()</c> static extension.
    /// </summary>
    private static INamedTypeSymbol? ResolveMockTarget(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        if (IsMockEntryPointMethod(methodSymbol))
        {
            return methodSymbol.TypeArguments.Length == 1
                ? methodSymbol.TypeArguments[0] as INamedTypeSymbol
                : null;
        }

        // Generated per-type entry point: `T.Mock()`. The generated static extension class always
        // lives in namespace TUnit.Mocks; its members bind through a compiler-synthesised nested
        // extension type, so match on the namespace rather than the immediate containing type.
        if (methodSymbol is not { Name: "Mock", IsStatic: true }
            || !IsTUnitMocksNamespace(methodSymbol.ContainingNamespace)
            || invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return null;
        }

        return context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken).Symbol
            as INamedTypeSymbol;
    }

    private static bool IsTUnitMocksNamespace(INamespaceSymbol? ns)
        => ns is { Name: "Mocks", ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true } };

    private static bool IsMockEntryPointMethod(IMethodSymbol method)
    {
        return method.Name is "Of" or "Wrap"
               && method.ContainingType is { Name: "Mock" or "MockRepository", ContainingNamespace: { Name: "Mocks", ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true } } }
               && method.IsGenericMethod;
    }

    /// <summary>
    /// Mirrors the generator's constructor discovery: a generated subclass can chain to any
    /// constructor that is not private, and not internal-without-access from another assembly.
    /// Protected (and protected internal) constructors are reachable precisely because the
    /// generated impl derives from the target.
    /// </summary>
    private static bool HasAccessibleConstructor(INamedTypeSymbol type, IAssemblySymbol compilationAssembly)
    {
        return type.InstanceConstructors.Any(ctor => IsChainable(ctor, compilationAssembly));
    }

    private static bool IsChainable(IMethodSymbol ctor, IAssemblySymbol compilationAssembly)
    {
        if (ctor.DeclaredAccessibility == Accessibility.Private)
        {
            return false;
        }

        var declaringAssembly = ctor.ContainingAssembly;
        if (declaringAssembly is null || SymbolEqualityComparer.Default.Equals(declaringAssembly, compilationAssembly))
        {
            return true;
        }

        return ctor.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.ProtectedAndInternal)
               || declaringAssembly.GivesAccessTo(compilationAssembly);
    }
}
