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

        // Generated per-type entry point: `T.Mock()`.
        if (!IsGeneratedStaticEntryPoint(methodSymbol)
            || invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return null;
        }

        return context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken).Symbol
            as INamedTypeSymbol;
    }

    /// <summary>
    /// Matches the <c>Mock()</c> the generator emits: a static member of a C# 14
    /// <c>extension(T)</c> block inside a <c>*_MockStaticExtension</c> class in namespace
    /// <c>TUnit.Mocks</c>. Roslyn reports the member's immediate containing type as the
    /// synthesised, unnamed extension type, so the check walks out to the declaring class —
    /// matching on the namespace alone would claim any static <c>Mock()</c> a consumer happens to
    /// declare there.
    /// </summary>
    private static bool IsGeneratedStaticEntryPoint(IMethodSymbol method)
    {
        if (method is not { Name: "Mock", IsStatic: true } || !IsTUnitMocksNamespace(method.ContainingNamespace))
        {
            return false;
        }

        for (var containing = method.ContainingType; containing is not null; containing = containing.ContainingType)
        {
            if (containing.Name.EndsWith("_MockStaticExtension", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
    /// Mirrors the generator's constructor discovery — <c>MemberDiscovery.DiscoverConstructors</c>
    /// → <c>IsMemberAccessible</c> → <c>AreMemberSignatureTypesAccessible</c>. A generated subclass
    /// can chain to a constructor only when the constructor itself is reachable AND every one of
    /// its parameter types is: the generator drops a constructor whose signature mentions an
    /// inaccessible type, and a target left with none is exactly the case this rule reports.
    /// Keep the two in step; they live in separate assemblies with no shared project.
    /// Protected (and protected internal) constructors are reachable precisely because the
    /// generated impl derives from the target.
    /// </summary>
    private static bool HasAccessibleConstructor(INamedTypeSymbol type, IAssemblySymbol compilationAssembly)
    {
        return type.InstanceConstructors.Any(ctor => IsChainable(ctor, compilationAssembly));
    }

    private static bool IsChainable(IMethodSymbol ctor, IAssemblySymbol compilationAssembly)
    {
        // DiscoverConstructors rejects private constructors outright, same-assembly or not —
        // a subclass can never chain to one.
        if (ctor.DeclaredAccessibility == Accessibility.Private)
        {
            return false;
        }

        return IsAssemblyReachable(ctor.DeclaredAccessibility, ctor.ContainingAssembly, compilationAssembly)
               && ctor.Parameters.All(p => IsTypeAccessible(p.Type, compilationAssembly));
    }

    /// <summary>
    /// Whether a symbol with this accessibility, declared in <paramref name="declaringAssembly"/>,
    /// is reachable from <paramref name="compilationAssembly"/>. Anything in the same assembly is;
    /// across assemblies only <c>internal</c> and <c>private protected</c> are gated (and then only
    /// without InternalsVisibleTo).
    /// </summary>
    private static bool IsAssemblyReachable(Accessibility accessibility, IAssemblySymbol? declaringAssembly, IAssemblySymbol compilationAssembly)
    {
        if (declaringAssembly is null || SymbolEqualityComparer.Default.Equals(declaringAssembly, compilationAssembly))
        {
            return true;
        }

        if (accessibility == Accessibility.Private)
        {
            return false;
        }

        return accessibility is not (Accessibility.Internal or Accessibility.ProtectedAndInternal)
               || declaringAssembly.GivesAccessTo(compilationAssembly);
    }

    private static bool IsTypeAccessible(ITypeSymbol type, IAssemblySymbol compilationAssembly)
    {
        // Type parameters are always accessible.
        if (type is ITypeParameterSymbol)
        {
            return true;
        }

        // Pointer types can't appear in a generated override signature, even same-assembly.
        if (type is IPointerTypeSymbol or IFunctionPointerTypeSymbol)
        {
            return false;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return IsTypeAccessible(arrayType.ElementType, compilationAssembly);
        }

        if (!IsAssemblyReachable(type.DeclaredAccessibility, type.ContainingAssembly, compilationAssembly))
        {
            return false;
        }

        return type is not INamedTypeSymbol namedType
               || namedType.TypeArguments.All(arg => IsTypeAccessible(arg, compilationAssembly));
    }
}
