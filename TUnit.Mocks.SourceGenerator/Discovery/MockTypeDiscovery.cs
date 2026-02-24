using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Mocks.SourceGenerator.Extensions;
using TUnit.Mocks.SourceGenerator.Models;
using System.Collections.Immutable;

namespace TUnit.Mocks.SourceGenerator.Discovery;

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
        // Also match X.Wrap(instance) where T is inferred (IdentifierNameSyntax).
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        // Generic name syntax: Mock.Of<T>(), Mock.Wrap<T>(instance)
        if (memberAccess.Name is GenericNameSyntax genericName)
        {
            var methodName = genericName.Identifier.ValueText;
            return methodName is "Of" or "OfPartial" or "OfDelegate" or "Wrap";
        }

        // Simple name syntax: Mock.Wrap(instance) with type inference
        if (memberAccess.Name is IdentifierNameSyntax identifierName)
        {
            return identifierName.Identifier.ValueText is "Wrap";
        }

        return false;
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

        // Verify this is TUnit.Mocks.Mock.Of<T>() / OfPartial<T>()
        // or TUnit.Mocks.MockRepository.Of<T>() / OfPartial<T>()
        var containingTypeName = method.ContainingType?.Name;
        if ((containingTypeName != "Mock" && containingTypeName != "MockRepository") ||
            method.ContainingNamespace?.ToDisplayString() != "TUnit.Mocks")
            return ImmutableArray<MockTypeModel>.Empty;

        var isPartialMock = method.Name == "OfPartial";
        var isDelegateMock = method.Name == "OfDelegate";
        var isWrapMock = method.Name == "Wrap";

        if (method.TypeArguments.Length == 0)
            return ImmutableArray<MockTypeModel>.Empty;

        var typeToMock = method.TypeArguments[0];
        if (typeToMock is not INamedTypeSymbol namedType)
            return ImmutableArray<MockTypeModel>.Empty;

        // Delegate mocking
        if (isDelegateMock)
        {
            if (namedType.TypeKind != TypeKind.Delegate)
                return ImmutableArray<MockTypeModel>.Empty;

            var delegateModel = BuildDelegateTypeModel(namedType);
            return delegateModel is not null
                ? ImmutableArray.Create(delegateModel)
                : ImmutableArray<MockTypeModel>.Empty;
        }

        // Wrap mock: generates a wrapper around a real instance
        if (isWrapMock)
        {
            // Wrap only works with classes (not interfaces, not sealed, not structs)
            if (namedType.TypeKind != TypeKind.Class || namedType.IsSealed || namedType.IsValueType)
                return ImmutableArray<MockTypeModel>.Empty;

            // Wrap uses the same model as partial mock but with IsWrapMock flag
            var wrapModel = BuildSingleTypeModel(namedType, isPartialMock: true);
            if (wrapModel is null)
                return ImmutableArray<MockTypeModel>.Empty;

            // Set IsWrapMock flag
            wrapModel = wrapModel with { IsWrapMock = true };
            return ImmutableArray.Create(wrapModel);
        }

        // Can't mock sealed classes or structs (analyzers catch this, but skip generation)
        if (namedType.IsSealed && namedType.TypeKind != TypeKind.Interface)
            return ImmutableArray<MockTypeModel>.Empty;
        if (namedType.IsValueType)
            return ImmutableArray<MockTypeModel>.Empty;

        // Single-type mock: build one model + transitive interface return types
        if (method.TypeArguments.Length == 1)
        {
            var model = BuildSingleTypeModel(namedType, isPartialMock);
            if (model is null)
                return ImmutableArray<MockTypeModel>.Empty;

            // Discover transitive interface return types for auto-mocking support
            var visited = new HashSet<string>();
            var transitiveModels = DiscoverTransitiveInterfaceTypes(namedType, visited, maxDepth: 3);

            if (transitiveModels.Count == 0)
                return ImmutableArray.Create(model);

            var builder = ImmutableArray.CreateBuilder<MockTypeModel>(1 + transitiveModels.Count);
            builder.Add(model);
            builder.AddRange(transitiveModels);
            return builder.MoveToImmutable();
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

    /// <summary>
    /// Walks the members of a type and discovers interface return types that need mock factories
    /// generated for auto-mocking support. Recurses up to maxDepth levels.
    /// </summary>
    private static List<MockTypeModel> DiscoverTransitiveInterfaceTypes(
        INamedTypeSymbol type, HashSet<string> visited, int maxDepth)
    {
        var results = new List<MockTypeModel>();
        if (maxDepth <= 0) return results;

        var fqn = type.GetFullyQualifiedName();
        visited.Add(fqn);

        // Collect all members from the type and its interfaces
        var members = new List<ISymbol>(type.GetMembers());
        foreach (var iface in type.AllInterfaces)
        {
            members.AddRange(iface.GetMembers());
        }

        foreach (var member in members)
        {
            ITypeSymbol? returnType = member switch
            {
                IMethodSymbol m when !m.ReturnsVoid => m.ReturnType,
                IPropertySymbol p => p.Type,
                _ => null
            };

            if (returnType is null) continue;

            // Unwrap Task<T>/ValueTask<T> to get the inner type
            returnType = UnwrapAsyncType(returnType);

            if (returnType is not INamedTypeSymbol namedReturn) continue;
            if (namedReturn.TypeKind != TypeKind.Interface) continue;

            // Skip BCL/system interfaces — they have members (indexers, explicit implementations)
            // that the mock generator cannot handle, and auto-mocking them is rarely useful.
            var ns = namedReturn.ContainingNamespace?.ToDisplayString() ?? "";
            if (IsFrameworkNamespace(ns))
                continue;

            var returnFqn = namedReturn.GetFullyQualifiedName();
            if (visited.Contains(returnFqn)) continue;
            visited.Add(returnFqn);

            var model = BuildSingleTypeModel(namedReturn, isPartialMock: false);
            if (model is null) continue;

            results.Add(model);
            // Recurse into the transitive type's members
            results.AddRange(DiscoverTransitiveInterfaceTypes(namedReturn, visited, maxDepth - 1));
        }

        return results;
    }

    /// <summary>
    /// Unwraps Task&lt;T&gt; / ValueTask&lt;T&gt; to get the inner type T.
    /// Returns the original type if it's not an async wrapper.
    /// </summary>
    private static ITypeSymbol UnwrapAsyncType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var constructedName = named.ConstructedFrom.ToDisplayString();
            if (constructedName is "System.Threading.Tasks.Task<TResult>"
                or "System.Threading.Tasks.ValueTask<TResult>")
            {
                return named.TypeArguments[0];
            }
        }
        return type;
    }

    private static bool IsFrameworkNamespace(string ns) =>
        ns == "System"    || ns.StartsWith("System.") ||
        ns == "Microsoft" || ns.StartsWith("Microsoft.") ||
        ns == "Windows"   || ns.StartsWith("Windows.");

    private static MockTypeModel? BuildDelegateTypeModel(INamedTypeSymbol delegateType)
    {
        var invokeMethod = delegateType.DelegateInvokeMethod;
        if (invokeMethod is null)
            return null;

        int memberIdCounter = 0;
        var methodModel = MemberDiscovery.CreateDelegateInvokeModel(invokeMethod, ref memberIdCounter);

        return new MockTypeModel
        {
            FullyQualifiedName = delegateType.GetFullyQualifiedName(),
            Name = delegateType.Name,
            Namespace = delegateType.ContainingNamespace?.ToDisplayString() ?? "",
            IsInterface = false,
            IsAbstract = false,
            IsPartialMock = false,
            IsDelegateType = true,
            Methods = new EquatableArray<MockMemberModel>(ImmutableArray.Create(methodModel)),
            Properties = EquatableArray<MockMemberModel>.Empty,
            Events = EquatableArray<MockEventModel>.Empty,
            AllInterfaces = EquatableArray<string>.Empty,
        };
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
