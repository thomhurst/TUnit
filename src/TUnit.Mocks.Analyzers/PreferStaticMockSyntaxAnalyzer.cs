using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Mocks.Analyzers;

/// <summary>
/// Reports <c>Mock.Of&lt;T&gt;()</c> and <c>Mock.Of&lt;T&gt;(behavior)</c> calls that can be written
/// as <c>T.Mock()</c> / <c>T.Mock(behavior)</c>. Only reports where the source generator emits the
/// <c>T.Mock()</c> extension for <c>T</c>, so the code fix never produces code that doesn't compile.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PreferStaticMockSyntaxAnalyzer : DiagnosticAnalyzer
{
    // C# 14 introduced extension members, which T.Mock() is built on.
    private const int CSharp14 = 1400;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.TM010_PreferStaticMockSyntax);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            if (compilationContext.Compilation is not CSharpCompilation { LanguageVersion: var languageVersion }
                || (int)languageVersion < CSharp14)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Cheap syntactic checks before binding: Of<T>() with a single type argument the fix can
        // move in front of `.Mock()`, and at most one argument (the behavior).
        if (GetOfName(invocation) is not { TypeArgumentList.Arguments: [var typeArgumentSyntax] }
            || !IsExpressibleAsReceiver(typeArgumentSyntax)
            || invocation.ArgumentList.Arguments.Count > 1)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
            || !IsMockOf(method)
            || method.TypeArguments is not [INamedTypeSymbol mockedType]
            || !HasStaticMockEquivalent(method))
        {
            return;
        }

        var compilation = context.SemanticModel.Compilation;

        if (!IsStaticMockGeneratedFor(mockedType, compilation))
        {
            return;
        }

        // Interfaces get a generated wrapper type that derives from Mock<T>. Wherever the call's
        // type is inferred (a `var` local, an implicitly typed array, a lambda's return type,
        // generic type inference), that inferred type would change, and later code relying on it
        // being Mock<T> could stop compiling after the fix.
        if (mockedType.TypeKind == TypeKind.Interface
            && MayChangeInferredType(invocation, typeArgumentSyntax, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        // The fix writes the type argument as the receiver of `.Mock()`. In expression position a
        // simple name can bind to something else first (e.g. a property named like the type).
        if (!ReceiverBindsToType(typeArgumentSyntax, mockedType, invocation.SpanStart, context.SemanticModel))
        {
            return;
        }

        // T.Mock() only binds where the TUnit.Mocks namespace is imported. When the fix has to add
        // `using TUnit.Mocks;`, don't report if that would make an existing simple name ambiguous.
        if (!IsNamespaceInScope(method.ContainingNamespace, invocation.SpanStart, context.SemanticModel)
            && ImportWouldCauseAmbiguity(method.ContainingNamespace, invocation.SyntaxTree.GetRoot(context.CancellationToken), context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rules.TM010_PreferStaticMockSyntax,
                invocation.GetLocation(),
                mockedType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static GenericNameSyntax? GetOfName(InvocationExpressionSyntax invocation)
    {
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } memberAccess => memberAccess.Name,
            GenericNameSyntax genericName => genericName,
            _ => null,
        };

        return name is GenericNameSyntax { Identifier.ValueText: "Of" } ofName ? ofName : null;
    }

    /// <summary>
    /// Whether the type argument can be written as the receiver of <c>.Mock()</c>. Names
    /// (simple, generic, qualified, alias-qualified) and keywords like <c>object</c> can; a nullable
    /// annotation is dropped by the fix.
    /// </summary>
    internal static bool IsExpressibleAsReceiver(TypeSyntax typeSyntax)
    {
        if (typeSyntax is NullableTypeSyntax nullable)
        {
            typeSyntax = nullable.ElementType;
        }

        return typeSyntax is NameSyntax or PredefinedTypeSyntax;
    }

    private static bool IsMockOf(IMethodSymbol method)
    {
        return method is { Name: "Of", IsStatic: true }
               && method.ContainingType is
               {
                   Name: "Mock",
                   ContainingType: null,
                   ContainingNamespace: { Name: "Mocks", ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true } },
               };
    }

    /// <summary>
    /// <c>T.Mock()</c> mirrors <c>Mock.Of&lt;T&gt;()</c> and <c>Mock.Of&lt;T&gt;(MockBehavior)</c>.
    /// Overloads taking a default value provider or constructor arguments are left alone.
    /// </summary>
    private static bool HasStaticMockEquivalent(IMethodSymbol method)
    {
        return method.Parameters switch
        {
            [] => true,
            [{ IsParams: false, Type: INamedTypeSymbol { Name: "MockBehavior" } behaviorType }] =>
                behaviorType.ContainingNamespace is { Name: "Mocks", ContainingNamespace: { Name: "TUnit", ContainingNamespace.IsGlobalNamespace: true } },
            _ => false,
        };
    }

    /// <summary>
    /// Mirrors the conditions under which TUnit.Mocks.SourceGenerator emits the static
    /// <c>T.Mock()</c> extension for a type discovered through <c>T.Mock()</c> itself.
    /// </summary>
    private static bool IsStaticMockGeneratedFor(INamedTypeSymbol type, Compilation compilation)
    {
        if (type.TypeKind is not (TypeKind.Interface or TypeKind.Class)
            || type.IsSealed
            || type.IsStatic
            || ContainsTypeParameterOrErrorType(type)
            || IsOrContainsFileLocalType(type)
            || !compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
        {
            return false;
        }

        // A member named Mock on the type itself would win over the extension member.
        if (HasMemberNamedMock(type))
        {
            return false;
        }

        if (type.TypeKind == TypeKind.Interface)
        {
            // The generator only emits the interface wrapper (and so T.Mock()) when no property
            // returns a ref struct. Static abstract members can't be used as type arguments at all.
            foreach (var @interface in type.AllInterfaces.Add(type))
            {
                foreach (var member in @interface.GetMembers())
                {
                    if (member.IsStatic && member.IsAbstract)
                    {
                        return false;
                    }

                    if (member is IPropertySymbol { IsStatic: false, Type.IsRefLikeType: true })
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static bool HasMemberNamedMock(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (!current.GetMembers("Mock").IsEmpty)
            {
                return true;
            }
        }

        foreach (var @interface in type.AllInterfaces)
        {
            if (!@interface.GetMembers("Mock").IsEmpty)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsTypeParameterOrErrorType(ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                return true;
            case IArrayTypeSymbol array:
                return ContainsTypeParameterOrErrorType(array.ElementType);
            case INamedTypeSymbol named:
                if (named.TypeKind == TypeKind.Error)
                {
                    return true;
                }

                if (named.ContainingType is { } containing && ContainsTypeParameterOrErrorType(containing))
                {
                    return true;
                }

                foreach (var typeArgument in named.TypeArguments)
                {
                    if (ContainsTypeParameterOrErrorType(typeArgument))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }

    private static bool IsOrContainsFileLocalType(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return IsOrContainsFileLocalType(array.ElementType);
            case INamedTypeSymbol named:
                if (named.IsFileLocal)
                {
                    return true;
                }

                if (named.ContainingType is { } containing && IsOrContainsFileLocalType(containing))
                {
                    return true;
                }

                foreach (var typeArgument in named.TypeArguments)
                {
                    if (IsOrContainsFileLocalType(typeArgument))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }

    /// <summary>
    /// For interfaces T.Mock() returns the generated wrapper type rather than Mock&lt;T&gt;. Returns
    /// true when the call sits where its type is inferred (a `var` local, an implicitly typed
    /// array, a lambda's return type, generic type inference, ...), so the rewrite could change
    /// a type later code depends on.
    /// </summary>
    private static bool MayChangeInferredType(
        InvocationExpressionSyntax invocation,
        TypeSyntax typeArgumentSyntax,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var node = MockTypeFlow.ClimbTypeFlow(invocation);
        var wrapperType = GetStaticMockReturnType(invocation, typeArgumentSyntax, semanticModel);

        if (node.Parent is EqualsValueClauseSyntax
            {
                Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: true } } declarator
            })
        {
            return HasTypeSensitiveReference(declarator, wrapperType, semanticModel, cancellationToken);
        }

        return !MockTypeFlow.IsTypeInsensitivePosition(node, wrapperType, semanticModel, cancellationToken);
    }

    /// <summary>
    /// The type the rewritten <c>T.Mock(...)</c> call would have, or null when it can't be bound
    /// here (for example because the fix still has to add <c>using TUnit.Mocks;</c>).
    /// </summary>
    private static ITypeSymbol? GetStaticMockReturnType(
        InvocationExpressionSyntax invocation,
        TypeSyntax typeArgumentSyntax,
        SemanticModel semanticModel)
    {
        if (typeArgumentSyntax is NullableTypeSyntax nullable)
        {
            typeArgumentSyntax = nullable.ElementType;
        }

        var rewritten = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ParseExpression(typeArgumentSyntax.WithoutTrivia().ToFullString()),
                SyntaxFactory.IdentifierName("Mock")),
            invocation.ArgumentList.WithoutTrivia());

        var type = semanticModel.GetSpeculativeTypeInfo(invocation.SpanStart, rewritten, SpeculativeBindingOption.BindAsExpression).Type;
        return type is null or IErrorTypeSymbol ? null : type;
    }

    /// <summary>
    /// Whether any use of the `var` local depends on its type staying Mock&lt;T&gt;: a write
    /// (assignment, tuple deconstruction, ref/out), or a read in a type-sensitive position such
    /// as <c>Task.FromResult(mock)</c>.
    /// </summary>
    private static bool HasTypeSensitiveReference(
        VariableDeclaratorSyntax declarator,
        ITypeSymbol? wrapperType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetDeclaredSymbol(declarator, cancellationToken) is not ILocalSymbol local)
        {
            return true;
        }

        // Top-level statements have no enclosing block: their locals span the compilation unit.
        SyntaxNode? scope = declarator.FirstAncestorOrSelf<BlockSyntax>();
        scope ??= declarator.FirstAncestorOrSelf<CompilationUnitSyntax>();

        if (scope is null)
        {
            return true;
        }

        foreach (var identifier in scope.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (identifier.Identifier.ValueText != local.Name
                || !SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol, local))
            {
                continue;
            }

            var reference = MockTypeFlow.ClimbTypeFlow(identifier);

            if (reference != identifier
                || MockTypeFlow.IsWrite(identifier)
                || !MockTypeFlow.IsTypeInsensitivePosition(identifier, wrapperType, semanticModel, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ReceiverBindsToType(
        TypeSyntax typeArgumentSyntax,
        INamedTypeSymbol mockedType,
        int position,
        SemanticModel semanticModel)
    {
        if (typeArgumentSyntax is NullableTypeSyntax nullable)
        {
            typeArgumentSyntax = nullable.ElementType;
        }

        var receiver = SyntaxFactory.ParseExpression(typeArgumentSyntax.WithoutTrivia().ToFullString());
        var symbol = semanticModel.GetSpeculativeSymbolInfo(position, receiver, SpeculativeBindingOption.BindAsExpression).Symbol;

        if (symbol is IAliasSymbol alias)
        {
            symbol = alias.Target;
        }

        return symbol is INamedTypeSymbol type
               && SymbolEqualityComparer.Default.Equals(type, mockedType);
    }

    private static bool IsNamespaceInScope(INamespaceSymbol mocksNamespace, int position, SemanticModel semanticModel)
    {
        for (var ns = semanticModel.GetEnclosingSymbol(position)?.ContainingNamespace; ns is { IsGlobalNamespace: false }; ns = ns.ContainingNamespace)
        {
            if (SymbolEqualityComparer.Default.Equals(ns, mocksNamespace))
            {
                return true;
            }
        }

        // A `using TUnit.Mocks;` (local or global) makes the namespace's types visible by simple
        // name. An alias such as `using M = TUnit.Mocks.Mock;` doesn't, and doesn't import
        // extension members either.
        foreach (var symbol in semanticModel.LookupNamespacesAndTypes(position, name: "Mock"))
        {
            if (symbol is INamedTypeSymbol { ContainingType: null } type
                && SymbolEqualityComparer.Default.Equals(type.ContainingNamespace, mocksNamespace))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether adding <c>using TUnit.Mocks;</c> to the file would make a simple name used
    /// anywhere in it (e.g. another mocking library's <c>Arg</c>) ambiguous. The fix adds the
    /// directive to the whole compilation unit, so every use in the file is checked, not just
    /// the call site.
    /// </summary>
    private static bool ImportWouldCauseAmbiguity(
        INamespaceSymbol mocksNamespace,
        SyntaxNode root,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        var typeNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in mocksNamespace.GetTypeMembers())
        {
            if (type.DeclaredAccessibility == Accessibility.Public)
            {
                typeNames.Add(type.Name);
            }
        }

        foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            if (!typeNames.Contains(name.Identifier.ValueText) || IsQualifiedOrBound(name))
            {
                continue;
            }

            if (semanticModel.GetAliasInfo(name, cancellationToken) is not null)
            {
                continue;
            }

            var symbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol;

            if (symbol is INamedTypeSymbol type
                && !SymbolEqualityComparer.Default.Equals(type.ContainingNamespace, mocksNamespace)
                && IsImportedFromOtherNamespace(type, name.SpanStart, semanticModel))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Names on the right of a dot (<c>X.Arg</c>, <c>x?.Arg</c>) or alias qualifier are looked up
    /// in their qualifier, not through using directives.
    /// </summary>
    private static bool IsQualifiedOrBound(SimpleNameSyntax name)
    {
        return name.Parent switch
        {
            QualifiedNameSyntax qualified => qualified.Right == name,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name == name,
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name == name,
            MemberBindingExpressionSyntax => true,
            _ => false,
        };
    }

    /// <summary>
    /// Nested types of enclosing classes and types of enclosing namespaces take precedence over a
    /// <c>using</c> namespace import, so only a type that is itself visible through another
    /// namespace import can clash.
    /// </summary>
    private static bool IsImportedFromOtherNamespace(INamedTypeSymbol type, int position, SemanticModel semanticModel)
    {
        if (type.ContainingType is not null)
        {
            return false;
        }

        for (var ns = semanticModel.GetEnclosingSymbol(position)?.ContainingNamespace; ns is not null; ns = ns.ContainingNamespace)
        {
            if (SymbolEqualityComparer.Default.Equals(ns, type.ContainingNamespace))
            {
                return false;
            }
        }

        return true;
    }

}
