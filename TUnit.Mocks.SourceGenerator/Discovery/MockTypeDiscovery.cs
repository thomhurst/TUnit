using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Mocks.SourceGenerator.Extensions;
using TUnit.Mocks.SourceGenerator.Models;
using System.Collections.Immutable;
using System.Linq;

namespace TUnit.Mocks.SourceGenerator.Discovery;

internal static class MockTypeDiscovery
{
    /// <summary>
    /// Syntax predicate: quick check if a node might be a Mock.Of&lt;T&gt;(),
    /// MockRepository.Of&lt;T&gt;(), etc. Zero allocations - string comparison only.
    /// </summary>
    public static bool IsMockOfInvocation(SyntaxNode node, CancellationToken ct)
    {
        // Match: X.Of<T>() or X.Of<T>(behavior, args) etc.
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
            return methodName is "Of" or "OfDelegate" or "Wrap";
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

        // When the interface has static abstract members, CS8920 prevents normal symbol
        // resolution (OverloadResolutionFailure). Fall back to CandidateSymbols.
        IMethodSymbol? method = symbolInfo.Symbol as IMethodSymbol;
        if (method is null && symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure)
        {
            method = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
        }

        if (method is null)
            return ImmutableArray<MockTypeModel>.Empty;

        // Verify this is TUnit.Mocks.Mock.Of<T>() or TUnit.Mocks.MockRepository.Of<T>()
        var containingTypeName = method.ContainingType?.Name;
        if ((containingTypeName != "Mock" && containingTypeName != "MockRepository") ||
            method.ContainingNamespace?.ToDisplayString() != "TUnit.Mocks")
            return ImmutableArray<MockTypeModel>.Empty;

        var isDelegateMock = method.Name == "OfDelegate";
        var isWrapMock = method.Name == "Wrap";

        if (method.TypeArguments.Length == 0)
            return ImmutableArray<MockTypeModel>.Empty;

        var typeToMock = method.TypeArguments[0];
        if (typeToMock is not INamedTypeSymbol namedType)
            return ImmutableArray<MockTypeModel>.Empty;

        // Skip error types (unresolvable type arguments, e.g. referencing a generated bridge type)
        if (namedType.TypeKind == TypeKind.Error)
            return ImmutableArray<MockTypeModel>.Empty;

        // Get the compilation assembly for accessibility checks on external types
        var compilationAssembly = context.SemanticModel.Compilation.Assembly;

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
            var wrapModel = BuildSingleTypeModel(namedType, isPartialMock: true, compilationAssembly);
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
        // Partial mock behavior is determined by type kind — classes get partial mocks automatically.
        var isPartialMock = namedType.TypeKind == TypeKind.Class;

        if (method.TypeArguments.Length == 1)
        {
            return BuildModelWithTransitiveDependencies(namedType, isPartialMock, compilationAssembly);
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
        var singleTypeModel = BuildSingleTypeModel(namedType, isPartialMock, compilationAssembly);
        if (singleTypeModel is null)
            return ImmutableArray<MockTypeModel>.Empty;

        // Build multi-type model (generates impl + factory only)
        var allTypes = new[] { namedType }.Concat(additionalTypes).ToArray();
        var (methods, properties, events) = MemberDiscovery.DiscoverMembersFromMultipleTypes(allTypes, compilationAssembly);

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
            Constructors = singleTypeModel.Constructors,
            HasStaticAbstractMembers = methods.Any(m => m.IsStaticAbstract) || properties.Any(p => p.IsStaticAbstract) || events.Any(e => e.IsStaticAbstract),
            IsPublic = IsEffectivelyPublic(namedType)
        };

        return ImmutableArray.Create(singleTypeModel, multiTypeModel);
    }

    /// <summary>
    /// Walks the members of a type and discovers interface return types that need mock factories
    /// generated for auto-mocking support. Recurses up to maxDepth levels.
    /// </summary>
    private static List<MockTypeModel> DiscoverTransitiveInterfaceTypes(
        INamedTypeSymbol type, HashSet<string> visited, int maxDepth, IAssemblySymbol? compilationAssembly)
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
            // Skip static members — static abstract return types should not be auto-mocked
            if (member.IsStatic) continue;

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

            // Skip interfaces that have static abstract members — using them as type arguments
            // in Mock<T>/MockEngine<T> triggers CS8920 because the static abstract members
            // don't have a most specific implementation in the interface.
            if (HasStaticAbstractMembers(namedReturn))
                continue;

            var returnFqn = namedReturn.GetFullyQualifiedName();
            if (visited.Contains(returnFqn)) continue;
            visited.Add(returnFqn);

            var model = BuildSingleTypeModel(namedReturn, isPartialMock: false, compilationAssembly);
            if (model is null) continue;

            results.Add(model);
            // Recurse into the transitive type's members
            results.AddRange(DiscoverTransitiveInterfaceTypes(namedReturn, visited, maxDepth - 1, compilationAssembly));
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

    /// <summary>
    /// Returns true if the interface (or any of its base interfaces) has static abstract members
    /// without a most specific implementation. Such interfaces cannot be used as generic type arguments
    /// (CS8920) and should not have transitive mock factories generated.
    /// </summary>
    private static bool HasStaticAbstractMembers(INamedTypeSymbol interfaceType)
    {
        // Check the interface itself
        foreach (var member in interfaceType.GetMembers())
        {
            if (member.IsStatic && member.IsAbstract)
                return true;
        }

        // Check all inherited interfaces
        foreach (var baseInterface in interfaceType.AllInterfaces)
        {
            foreach (var member in baseInterface.GetMembers())
            {
                if (member.IsStatic && member.IsAbstract)
                    return true;
            }
        }

        return false;
    }

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
            IsPublic = IsEffectivelyPublic(delegateType),
        };
    }

    private static ImmutableArray<MockTypeModel> BuildModelWithTransitiveDependencies(
        INamedTypeSymbol namedType, bool isPartialMock, IAssemblySymbol? compilationAssembly)
    {
        var model = BuildSingleTypeModel(namedType, isPartialMock, compilationAssembly);
        if (model is null)
            return ImmutableArray<MockTypeModel>.Empty;

        var visited = new HashSet<string>();
        var transitiveModels = DiscoverTransitiveInterfaceTypes(namedType, visited, maxDepth: 3, compilationAssembly);

        if (transitiveModels.Count == 0)
            return ImmutableArray.Create(model);

        var builder = ImmutableArray.CreateBuilder<MockTypeModel>(1 + transitiveModels.Count);
        builder.Add(model);
        builder.AddRange(transitiveModels);
        return builder.MoveToImmutable();
    }

    private static MockTypeModel? BuildSingleTypeModel(INamedTypeSymbol namedType, bool isPartialMock, IAssemblySymbol? compilationAssembly)
    {
        var (methods, properties, events) = MemberDiscovery.DiscoverMembers(namedType, compilationAssembly);

        // Discover constructors for partial mocks of classes
        var constructors = isPartialMock && namedType.TypeKind == TypeKind.Class
            ? MemberDiscovery.DiscoverConstructors(namedType, compilationAssembly)
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
            Constructors = constructors,
            HasStaticAbstractMembers = methods.Any(m => m.IsStaticAbstract) || properties.Any(p => p.IsStaticAbstract) || events.Any(e => e.IsStaticAbstract),
            IsPublic = IsEffectivelyPublic(namedType)
        };
    }

    /// <summary>
    /// True if every part of <paramref name="type"/>'s signature is publicly accessible: the
    /// type itself, every enclosing type, and (recursively) every generic type argument and
    /// array element. Mock wrappers built for types that are not effectively public must
    /// themselves be emitted as <c>internal</c> to avoid CS9338 / CS0051 — including the
    /// case where a public generic interface is closed over an internal type argument
    /// (e.g. <c>ILogger&lt;InternalClass&gt;</c>). See issues #5426 and #5453.
    /// </summary>
    private static bool IsEffectivelyPublic(ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                // Bound at use site by the consumer; not the discovery point's concern.
                return true;

            case IArrayTypeSymbol array:
                return IsEffectivelyPublic(array.ElementType);

            case INamedTypeSymbol named:
                for (INamedTypeSymbol? t = named; t is not null; t = t.ContainingType)
                {
                    if (t.DeclaredAccessibility != Accessibility.Public)
                        return false;
                }
                foreach (var typeArg in named.TypeArguments)
                {
                    if (!IsEffectivelyPublic(typeArg))
                        return false;
                }
                return true;

            default:
                // Pointers, function pointers, dynamic, error types — not expected in
                // mockable signatures.
                return true;
        }
    }

    // ─── T.Mock() static extension discovery ─────────────────────────

    /// <summary>
    /// Syntax predicate: matches T.Mock() — a static extension invocation where the
    /// left-hand side is a type name (not a variable/field access).
    /// Works for both interfaces (e.g. IFoo.Mock()) and classes (e.g. MyService.Mock()).
    /// </summary>
    public static bool IsMockExtensionInvocation(SyntaxNode node, CancellationToken ct)
    {
        if (node is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name: IdentifierNameSyntax { Identifier.ValueText: "Mock" },
                    Expression: var lhs
                }
            })
            return false;

        // Static extension calls use a type name on the left — never a variable or member access.
        // GenericNameSyntax handles IFoo<T>.Mock().
        return lhs is IdentifierNameSyntax or GenericNameSyntax or QualifiedNameSyntax or AliasQualifiedNameSyntax;
    }

    /// <summary>
    /// Semantic transform: resolve the left-hand side of T.Mock() to a type symbol.
    /// If it's a mockable type (interface or non-sealed class), build a MockTypeModel for it.
    /// </summary>
    public static ImmutableArray<MockTypeModel> TransformMockExtensionInvocation(
        GeneratorSyntaxContext context, CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

        // Resolve the LHS to a type symbol first (cheap lookup).
        var leftSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression, ct);
        if (leftSymbol.Symbol is not INamedTypeSymbol namedType)
            return ImmutableArray<MockTypeModel>.Empty;

        // Can't mock sealed classes, structs, or delegates via the extension
        if (namedType.IsValueType)
            return ImmutableArray<MockTypeModel>.Empty;
        if (namedType.IsSealed && namedType.TypeKind != TypeKind.Interface)
            return ImmutableArray<MockTypeModel>.Empty;
        if (namedType.TypeKind is not (TypeKind.Interface or TypeKind.Class))
            return ImmutableArray<MockTypeModel>.Empty;

        // Skip if .Mock() already resolves to a generated specialization (2nd incremental pass).
        // The generated per-type extension lives in a class named *_MockStaticExtension.
        // This covers both interfaces (wrapper return type in TUnit.Mocks.Generated) and
        // classes (Mock<T> return type in TUnit.Mocks).
        var invocationSymbol = context.SemanticModel.GetSymbolInfo(invocation, ct);
        if (invocationSymbol.Symbol is IMethodSymbol resolved
            && resolved.ContainingType?.Name is { } containingName
            && containingName.EndsWith("_MockStaticExtension"))
            return ImmutableArray<MockTypeModel>.Empty;

        var isPartialMock = namedType.TypeKind == TypeKind.Class;
        var compilationAssembly = context.SemanticModel.Compilation.Assembly;
        return BuildModelWithTransitiveDependencies(namedType, isPartialMock, compilationAssembly);
    }

    // ─── [assembly: GenerateMock(typeof(T))] discovery ────────────────────

    /// <summary>
    /// Semantic transform for <c>[assembly: GenerateMock(typeof(T))]</c>.
    /// Extracts the type argument and builds a <see cref="MockTypeModel"/>.
    /// </summary>
    public static ImmutableArray<MockTypeModel> TransformGenerateMockAttribute(
        GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        // The target symbol for an assembly attribute is the assembly itself
        // The attribute constructor argument is typeof(T)
        var compilationAssembly = context.SemanticModel.Compilation.Assembly;
        foreach (var attr in context.Attributes)
        {
            if (attr.AttributeClass?.Name is not ("GenerateMockAttribute" or "GenerateMock"))
                continue;
            if (attr.AttributeClass?.ContainingNamespace?.ToDisplayString() != "TUnit.Mocks")
                continue;

            if (attr.ConstructorArguments.Length != 1)
                continue;

            var typeArg = attr.ConstructorArguments[0];
            if (typeArg.Value is not INamedTypeSymbol namedType)
                continue;

            // Can't mock sealed classes or structs
            if (namedType.IsSealed && namedType.TypeKind != TypeKind.Interface)
                continue;
            if (namedType.IsValueType)
                continue;

            var model = BuildSingleTypeModel(namedType, isPartialMock: namedType.TypeKind == TypeKind.Class, compilationAssembly);
            if (model is null)
                continue;

            return ImmutableArray.Create(model);
        }

        return ImmutableArray<MockTypeModel>.Empty;
    }
}
