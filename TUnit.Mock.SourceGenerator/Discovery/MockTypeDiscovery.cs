using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Mock.SourceGenerator.Extensions;
using TUnit.Mock.SourceGenerator.Models;
using System.Collections.Immutable;

namespace TUnit.Mock.SourceGenerator.Discovery;

internal static class MockTypeDiscovery
{
    /// <summary>
    /// Syntax predicate: quick check if a node might be a Mock.Of&lt;T&gt;() or Mock.OfPartial&lt;T&gt;() call.
    /// Zero allocations - string comparison only.
    /// </summary>
    public static bool IsMockOfInvocation(SyntaxNode node, CancellationToken ct)
    {
        // Match: Mock.Of<T>() or Mock.Of<T>(behavior) or Mock.OfPartial<T>(...)
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (memberAccess.Name is not GenericNameSyntax genericName)
            return false;

        var methodName = genericName.Identifier.ValueText;
        if (methodName != "Of" && methodName != "OfPartial")
            return false;

        if (memberAccess.Expression is not IdentifierNameSyntax identifier)
            return false;

        return identifier.Identifier.ValueText == "Mock";
    }

    /// <summary>
    /// Semantic transform: resolve the type argument and build a MockTypeModel.
    /// </summary>
    public static MockTypeModel? TransformToModel(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, ct);
        if (symbolInfo.Symbol is not IMethodSymbol method)
            return null;

        // Verify this is TUnit.Mock.Mock.Of<T>() or TUnit.Mock.Mock.OfPartial<T>()
        if (method.ContainingType?.Name != "Mock" ||
            method.ContainingNamespace?.ToDisplayString() != "TUnit.Mock")
            return null;

        var isPartialMock = method.Name == "OfPartial";

        if (method.TypeArguments.Length != 1)
            return null;

        var typeToMock = method.TypeArguments[0];
        if (typeToMock is not INamedTypeSymbol namedType)
            return null;

        // Can't mock sealed classes or structs (analyzers catch this, but skip generation)
        if (namedType.IsSealed && namedType.TypeKind != TypeKind.Interface)
            return null;
        if (namedType.IsValueType)
            return null;

        var (methods, properties, events) = MemberDiscovery.DiscoverMembers(namedType);

        // Discover constructors for partial mocks of classes
        var constructors = isPartialMock && namedType.TypeKind == TypeKind.Class
            ? MemberDiscovery.DiscoverConstructors(namedType)
            : EquatableArray<MockConstructorModel>.Empty;

        return new MockTypeModel
        {
            FullyQualifiedName = namedType.GetFullyQualifiedName(),
            Name = namedType.Name,
            Namespace = namedType.ContainingNamespace?.ToDisplayString() ?? "",
            IsInterface = namedType.TypeKind == TypeKind.Interface,
            IsAbstract = namedType.IsAbstract,
            IsPartialMock = isPartialMock,
            Methods = methods,
            Properties = properties,
            Events = events,
            AllInterfaces = new EquatableArray<string>(
                namedType.AllInterfaces
                    .Select(i => i.GetFullyQualifiedName())
                    .ToImmutableArray()
            ),
            Constructors = constructors
        };
    }
}
