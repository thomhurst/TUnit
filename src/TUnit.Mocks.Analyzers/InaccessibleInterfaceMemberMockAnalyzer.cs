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

        if (ResolveMockTarget(context, invocation) is not { TypeKind: TypeKind.Interface } interfaceType)
        {
            return;
        }

        var inaccessibleMember = FindInaccessibleAbstractMember(interfaceType, context.Compilation);

        if (inaccessibleMember is null)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rules.TM007_CannotMockInterfaceWithInaccessibleMember,
                invocation.GetLocation(),
                interfaceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                inaccessibleMember.Name
            )
        );
    }

    /// <summary>
    /// Resolves the mocked type from either entry point: the generic <c>Mock.Of&lt;T&gt;()</c>
    /// form, or <c>T.Mock()</c>. The latter is matched syntactically as well as semantically —
    /// when the generator declines to emit the mock there is no <c>Mock()</c> member to bind to,
    /// which is exactly the case this diagnostic needs to explain.
    /// </summary>
    private static INamedTypeSymbol? ResolveMockTarget(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);

        if (symbolInfo.Symbol is IMethodSymbol methodSymbol && IsMockEntryPointMethod(methodSymbol))
        {
            return methodSymbol.TypeArguments.Length == 1
                ? methodSymbol.TypeArguments[0] as INamedTypeSymbol
                : null;
        }

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
    /// reachable. Mirrors <c>InterfaceImplementability</c> in the generator.
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

            if (!compilation.IsSymbolAccessibleWithin(member, compilation.Assembly))
            {
                return member;
            }
        }

        return null;
    }
}
