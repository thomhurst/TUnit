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
    /// Syntax predicate: quick check if a node might be a Mock.Of&lt;T&gt;(), Mock.OfPartial&lt;T&gt;(),
    /// or MockRepository.Of&lt;T&gt;() call. Zero allocations - string comparison only.
    /// </summary>
    public static bool IsMockOfInvocation(SyntaxNode node, CancellationToken ct)
    {
        // Match: X.Of<T>() or X.Of<T>(behavior) or X.OfPartial<T>(...)
        // where X can be "Mock" (static) or a MockRepository variable (instance).
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (memberAccess.Name is not GenericNameSyntax genericName)
            return false;

        var methodName = genericName.Identifier.ValueText;
        return methodName is "Of" or "OfPartial";
    }

    /// <summary>
    /// Semantic transform: resolve the type argument(s) and build MockTypeModel(s).
    /// For multi-interface calls (Mock.Of&lt;T1, T2&gt;()), returns both a single-type model
    /// (for setup/verify generation) and a multi-type model (for impl/factory generation).
    /// </summary>
    public static ImmutableArray<MockTypeModel> TransformToModels(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, ct);
        if (symbolInfo.Symbol is not IMethodSymbol method)
            return ImmutableArray<MockTypeModel>.Empty;

        // Verify this is TUnit.Mock.Mock.Of<T>() / OfPartial<T>()
        // or TUnit.Mock.MockRepository.Of<T>() / OfPartial<T>()
        var containingTypeName = method.ContainingType?.Name;
        if ((containingTypeName != "Mock" && containingTypeName != "MockRepository") ||
            method.ContainingNamespace?.ToDisplayString() != "TUnit.Mock")
            return ImmutableArray<MockTypeModel>.Empty;

        var isPartialMock = method.Name == "OfPartial";

        if (method.TypeArguments.Length == 0)
            return ImmutableArray<MockTypeModel>.Empty;

        var typeToMock = method.TypeArguments[0];
        if (typeToMock is not INamedTypeSymbol namedType)
            return ImmutableArray<MockTypeModel>.Empty;

        // Can't mock sealed classes or structs (analyzers catch this, but skip generation)
        if (namedType.IsSealed && namedType.TypeKind != TypeKind.Interface)
            return ImmutableArray<MockTypeModel>.Empty;
        if (namedType.IsValueType)
            return ImmutableArray<MockTypeModel>.Empty;

        // Single-type mock: build one model
        if (method.TypeArguments.Length == 1)
        {
            var model = BuildSingleTypeModel(namedType, isPartialMock);
            return model is not null
                ? ImmutableArray.Create(model)
                : ImmutableArray<MockTypeModel>.Empty;
        }

        // Multi-type mock: validate additional type args are all interfaces
        var additionalTypes = new List<INamedTypeSymbol>();
        for (int i = 1; i < method.TypeArguments.Length; i++)
        {
            if (method.TypeArguments[i] is not INamedTypeSymbol additionalType)
                return ImmutableArray<MockTypeModel>.Empty;
            if (additionalType.TypeKind != TypeKind.Interface)
                return ImmutableArray<MockTypeModel>.Empty;
            additionalTypes.Add(additionalType);
        }

        // Build single-type model for primary type (generates setup/verify/raise)
        var singleTypeModel = BuildSingleTypeModel(namedType, isPartialMock);
        if (singleTypeModel is null)
            return ImmutableArray<MockTypeModel>.Empty;

        // Build multi-type model (generates impl + factory only)
        var allTypes = new[] { namedType }.Concat(additionalTypes).ToArray();
        var (methods, properties, events) = MemberDiscovery.DiscoverMembersFromMultipleTypes(allTypes);

        var additionalInterfaceNames = ImmutableArray.CreateBuilder<string>(additionalTypes.Count);
        foreach (var t in additionalTypes)
        {
            additionalInterfaceNames.Add(t.GetFullyQualifiedName());
        }

        var multiTypeModel = new MockTypeModel
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
            AdditionalInterfaceNames = new EquatableArray<string>(additionalInterfaceNames.MoveToImmutable()),
            Constructors = singleTypeModel.Constructors
        };

        return ImmutableArray.Create(singleTypeModel, multiTypeModel);
    }

    private static MockTypeModel? BuildSingleTypeModel(INamedTypeSymbol namedType, bool isPartialMock)
    {
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
