using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Mocks.Analyzers;

/// <summary>
/// Reports TM007 when an interface is mocked but declares an abstract member this compilation
/// cannot access — since C# 8 an interface may have non-public abstract members, and no type
/// outside the declaring assembly can implement them. The generator skips such interfaces, so
/// this diagnostic is what turns the attempt into an actionable compile error. See issue #6491.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InaccessibleInterfaceMemberMockAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.TM007_CannotMockInterfaceWithInaccessibleMember);

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

        foreach (var target in ResolveMockTargets(context, invocation))
        {
            if (target.TypeKind != TypeKind.Interface)
            {
                continue;
            }

            var inaccessibleMember = FindInaccessibleAbstractMember(target, context.Compilation);

            if (inaccessibleMember is null)
            {
                continue;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rules.TM007_CannotMockInterfaceWithInaccessibleMember,
                    invocation.GetLocation(),
                    target.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    inaccessibleMember.Name
                )
            );
        }
    }

    /// <summary>
    /// Resolves the mocked type(s) from either entry point: the generic <c>Mock.Of&lt;T&gt;()</c>
    /// form — including the multi-interface <c>Mock.Of&lt;T1, T2&gt;()</c> shape, where any type
    /// argument makes the whole combo unmockable — or <c>T.Mock()</c>. The latter is matched
    /// syntactically as well as semantically: when the generator declines to emit the mock there
    /// is no <c>Mock()</c> member to bind to, which is exactly the case this diagnostic explains.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> ResolveMockTargets(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);

        if (symbolInfo.Symbol is IMethodSymbol methodSymbol && IsMockEntryPointMethod(methodSymbol))
        {
            return methodSymbol.TypeArguments.OfType<INamedTypeSymbol>();
        }

        return ResolveStaticEntryPointTarget(context, invocation) is { } target
            ? new[] { target }
            : Enumerable.Empty<INamedTypeSymbol>();
    }

    private static INamedTypeSymbol? ResolveStaticEntryPointTarget(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {

        if (invocation is not
            {
                ArgumentList.Arguments.Count: 0,
                Expression: MemberAccessExpressionSyntax
                {
                    Name: IdentifierNameSyntax { Identifier.ValueText: "Mock" }
                } memberAccess
            })
        {
            return null;
        }

        return context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken).Symbol
            as INamedTypeSymbol;
    }

    private static bool IsMockEntryPointMethod(IMethodSymbol method)
    {
        return method.Name is "Of"
               && method.ContainingType is { Name: "Mock" or "MockRepository", ContainingNamespace: { Name: "Mocks", ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true } } }
               && method.IsGenericMethod;
    }

    /// <summary>
    /// Returns the first abstract member of the interface (or one of its base interfaces) that an
    /// implementer declared in this compilation could not access, or null when all of them are
    /// reachable. Mirrors <c>InterfaceImplementability</c> in the generator, which delegates to
    /// <c>MemberDiscovery.IsMemberAccessible</c> — keep the two in step. The analyzer and the
    /// generator ship as separate assemblies with no shared project, so this is a deliberate
    /// second copy of a deliberately small rule.
    /// </summary>
    private static ISymbol? FindInaccessibleAbstractMember(INamedTypeSymbol interfaceType, Compilation compilation)
    {
        return FindInDeclaredMembers(interfaceType, compilation)
            ?? interfaceType.AllInterfaces
                .Select(i => FindInDeclaredMembers(i, compilation))
                .FirstOrDefault(m => m is not null);
    }

    private static ISymbol? FindInDeclaredMembers(INamedTypeSymbol interfaceType, Compilation compilation)
    {
        foreach (var member in interfaceType.GetMembers())
        {
            if (!member.IsAbstract)
            {
                continue;
            }

            // Accessors carry their property's/event's accessibility — report the member itself.
            if (member is IMethodSymbol { AssociatedSymbol: not null })
            {
                continue;
            }

            if (!IsImplementableFrom(member, compilation.Assembly))
            {
                return member;
            }
        }

        return null;
    }

    /// <summary>
    /// True when a type declared in <paramref name="compilationAssembly"/> could provide this
    /// interface member. Only <c>internal</c> and <c>private protected</c> are assembly-scoped;
    /// <c>protected</c> and <c>protected internal</c> members are reachable from any assembly
    /// through explicit interface implementation, so they must not be treated as blockers.
    /// <c>Compilation.IsSymbolAccessibleWithin</c> is deliberately not used here: given an
    /// assembly as the "within" context it reports <c>protected</c> as inaccessible, which is the
    /// wrong question for an implementer.
    /// </summary>
    private static bool IsImplementableFrom(ISymbol member, IAssemblySymbol compilationAssembly)
    {
        if (member.DeclaredAccessibility is not (Accessibility.Internal or Accessibility.ProtectedAndInternal))
        {
            return true;
        }

        var declaringAssembly = member.ContainingAssembly;

        return declaringAssembly is null
            || SymbolEqualityComparer.Default.Equals(declaringAssembly, compilationAssembly)
            || declaringAssembly.GivesAccessTo(compilationAssembly);
    }
}
