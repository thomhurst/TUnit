using Microsoft.CodeAnalysis;
using TUnit.Mocks.SourceGenerator.Extensions;
using TUnit.Mocks.SourceGenerator.Models;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static TUnit.Mocks.SourceGenerator.IdentifierEscaping;

namespace TUnit.Mocks.SourceGenerator.Discovery;

/// <summary>
/// Enumerates all mockable members of a type symbol.
/// Walks AllInterfaces for inherited members, detects overlapping signatures,
/// and handles default interface methods, properties, events, and indexers.
/// </summary>
internal static class MemberDiscovery
{
    /// <summary>Sentinel entry for non-mockable members (sealed, non-virtual) that block the interface loop.</summary>
    private static readonly (int Index, ITypeSymbol? ReturnType) NonMockableEntry = (-1, null);

    /// <summary>
    /// Mutable accumulation state shared by the single- and multi-type member walks.
    /// Both walks run the exact same collector over the primary type, so the primary's
    /// members receive identical dense member IDs in both models — the setup extensions
    /// are generated from the single-type model but dispatch against the multi impl's engine,
    /// so this prefix-ID invariant is load-bearing.
    /// </summary>
    private sealed class DiscoveryState
    {
        public readonly List<MockMemberModel> Methods = new();
        public readonly List<MockMemberModel> Properties = new();
        public readonly List<MockEventModel> Events = new();
        public readonly Dictionary<string, (int Index, ITypeSymbol? ReturnType)> SeenMethods = new();
        public readonly HashSet<string> SeenFullMethods = new();
        public readonly Dictionary<string, int?> SeenProperties = new();
        public readonly HashSet<string> SeenEvents = new();
        /// <summary>
        /// Explicit interface impls collected for members blocked by a non-mockable class member,
        /// keyed "interfaceFqn|memberKey". Prevents duplicate explicit impls when the same base
        /// interface is reachable through multiple additional interfaces.
        /// </summary>
        public readonly HashSet<string> SeenExplicitImpls = new();
        public int MemberIdCounter;

        public (EquatableArray<MockMemberModel> Methods, EquatableArray<MockMemberModel> Properties, EquatableArray<MockEventModel> Events) ToResult() => (
            new EquatableArray<MockMemberModel>(Methods.ToImmutableArray()),
            new EquatableArray<MockMemberModel>(Properties.ToImmutableArray()),
            new EquatableArray<MockEventModel>(Events.ToImmutableArray())
        );
    }

    public static (EquatableArray<MockMemberModel> Methods, EquatableArray<MockMemberModel> Properties, EquatableArray<MockEventModel> Events)
        DiscoverMembers(ITypeSymbol typeSymbol, IAssemblySymbol? compilationAssembly, Compilation compilation)
    {
        var state = new DiscoveryState();
        CollectMembers(typeSymbol, compilationAssembly, compilation, state, ownerTypeIndex: 0, primaryClassSymbol: null);
        return state.ToResult();
    }

    /// <summary>
    /// Discovers members from multiple type symbols, merging and deduplicating across all.
    /// Used for multi-interface mocks like Mock.Of&lt;T1, T2&gt;(). Dedup is first-wins, so members
    /// shared with the primary keep <c>OwnerTypeIndex == 0</c> and a single setup extension.
    /// Note: the first element may be a class when invoked from
    /// <c>MockTypeDiscovery.TransformToModels</c> with <c>isPartialMock == true</c>, so the
    /// <see cref="TryCollectStaticAbstractFromInterface"/> TypeKind guard is genuinely required.
    /// </summary>
    public static (EquatableArray<MockMemberModel> Methods, EquatableArray<MockMemberModel> Properties, EquatableArray<MockEventModel> Events)
        DiscoverMembersFromMultipleTypes(INamedTypeSymbol[] typeSymbols, IAssemblySymbol? compilationAssembly, Compilation compilation)
    {
        var state = new DiscoveryState();
        var primaryClass = typeSymbols[0].TypeKind == TypeKind.Class ? typeSymbols[0] : null;
        for (int i = 0; i < typeSymbols.Length; i++)
        {
            CollectMembers(typeSymbols[i], compilationAssembly, compilation, state,
                ownerTypeIndex: i, primaryClassSymbol: i > 0 ? primaryClass : null);
        }
        return state.ToResult();
    }

    /// <summary>
    /// Returns true when an additional-interface member must be emitted as an explicit interface
    /// implementation because the class primary already implements it (explicitly or non-virtually) —
    /// explicit re-implementation is the only way the mock can intercept interface dispatch
    /// (e.g. DbContext explicitly implements IInfrastructure&lt;IServiceProvider&gt;.Instance).
    /// </summary>
    private static bool RequiresExplicitImpl(INamedTypeSymbol? primaryClassSymbol, ISymbol interfaceMember)
        => primaryClassSymbol is not null
            && primaryClassSymbol.FindImplementationForInterfaceMember(interfaceMember) is not null;

    /// <summary>
    /// Records that the member at <paramref name="index"/> must additionally be forwarded as an
    /// explicit implementation of <paramref name="interfaceFqn"/> in the generated wrapper type.
    /// Used when an identically-signed member is dropped during dedup but represents a distinct
    /// interface slot (a base member hidden by <c>new</c>, or one inherited from multiple
    /// interfaces): the shared impl satisfies every slot implicitly, but the wrapper forwards
    /// explicitly and an explicit impl satisfies only the one slot it names (#6252). No-op when
    /// the interface already matches the member's own slot or is already recorded. Only meaningful
    /// for single-interface mocks — the only ones with a wrapper.
    /// </summary>
    /// <param name="slotHasGetter">/<param name="slotHasSetter">: the accessors declared by the
    /// shadowed slot itself, so the wrapper forward for it emits only those (asymmetric <c>new</c>
    /// hiding — CS0550, #6263). Both true for methods, where accessor presence is irrelevant.</param>
    private static void RecordAdditionalWrapperInterface(List<MockMemberModel> members, int index, string interfaceFqn,
        bool slotHasGetter, bool slotHasSetter)
    {
        var existing = members[index];
        var ownSlot = existing.ExplicitInterfaceName ?? existing.DeclaringInterfaceName;
        if (ownSlot == interfaceFqn) return;
        var array = existing.AdditionalExplicitSlots.AsImmutableArray();
        if (array.Any(s => s.InterfaceName == interfaceFqn)) return;
        var slot = new MockExplicitInterfaceSlot
        {
            InterfaceName = interfaceFqn,
            HasGetter = slotHasGetter,
            HasSetter = slotHasSetter
        };
        members[index] = existing with
        {
            AdditionalExplicitSlots = new EquatableArray<MockExplicitInterfaceSlot>(array.Add(slot))
        };
    }

    /// <summary>Event counterpart of <see cref="RecordAdditionalWrapperInterface"/>. Locates the
    /// surviving (non-static) event model by name, since the event seen-set is keyed by name only.</summary>
    private static void RecordAdditionalWrapperInterfaceForEvent(List<MockEventModel> events, string eventName, string interfaceFqn)
    {
        for (int i = 0; i < events.Count; i++)
        {
            var e = events[i];
            if (e.IsStaticAbstract || e.Name != eventName) continue;
            var updated = AppendDistinctSlot(e.AdditionalExplicitInterfaceNames,
                e.ExplicitInterfaceName ?? e.DeclaringInterfaceName, interfaceFqn, out var changed);
            if (changed) events[i] = e with { AdditionalExplicitInterfaceNames = updated };
            return;
        }
    }

    /// <summary>Returns <paramref name="slots"/> with <paramref name="interfaceFqn"/> appended,
    /// setting <paramref name="changed"/>. No-op (returns the input) when <paramref name="interfaceFqn"/>
    /// already is the member's own slot (<paramref name="ownSlot"/>) or is already recorded — so the
    /// caller can skip allocating a new model record.</summary>
    private static EquatableArray<string> AppendDistinctSlot(
        EquatableArray<string> slots, string? ownSlot, string interfaceFqn, out bool changed)
    {
        changed = false;
        if (ownSlot == interfaceFqn) return slots;
        var array = slots.AsImmutableArray();
        if (array.Contains(interfaceFqn)) return slots;
        changed = true;
        return new EquatableArray<string>(array.Add(interfaceFqn));
    }

    private static MockMemberModel Tag(MockMemberModel model, int ownerTypeIndex)
        => ownerTypeIndex == 0 ? model : model with { OwnerTypeIndex = ownerTypeIndex };

    private static MockEventModel Tag(MockEventModel model, int ownerTypeIndex)
        => ownerTypeIndex == 0 ? model : model with { OwnerTypeIndex = ownerTypeIndex };

    /// <summary>
    /// Collects the mockable members of one type into the shared state.
    /// <paramref name="ownerTypeIndex"/>: 0 = the primary type, n = 1-based index into the
    /// additional interfaces of a multi-type mock.
    /// <paramref name="primaryClassSymbol"/>: non-null only when walking an additional interface
    /// of a class-primary multi mock — members the class already implements are then collected as
    /// explicit interface impls (interception), instead of skipped (the single-type behavior, #5673).
    /// </summary>
    private static void CollectMembers(
        ITypeSymbol typeSymbol,
        IAssemblySymbol? compilationAssembly,
        Compilation compilation,
        DiscoveryState state,
        int ownerTypeIndex,
        INamedTypeSymbol? primaryClassSymbol)
    {
        // Collect all interfaces to scan
        var interfaces = typeSymbol.TypeKind == TypeKind.Interface
            ? new[] { typeSymbol }.Concat(typeSymbol.AllInterfaces)
            : typeSymbol.AllInterfaces.AsEnumerable();

        // If it's a class, also include its own members
        if (typeSymbol.TypeKind == TypeKind.Class)
        {
            ProcessClassMembers(typeSymbol, compilationAssembly, compilation, state);
        }

        foreach (var iface in interfaces)
        {
            var interfaceFqn = iface.GetFullyQualifiedName();

            foreach (var member in iface.GetMembers())
            {
                if (member.IsStatic)
                {
                    TryCollectStaticAbstractFromInterface(member, typeSymbol, interfaceFqn, state, compilation);
                    continue;
                }

                // For class partial mocks, the base class already implements (or inherits) all
                // interface members — re-emitting them as `public override` fails to compile
                // when the base impl is non-virtual or explicit (#5673:
                // EntityEntry explicitly implements IInfrastructure<InternalEntityEntry>.Instance).
                // The inherited impl satisfies the interface; the mock only needs to override
                // what the class walk already collected (virtual/abstract/override members).
                // (Additional-interface walks never take this branch — there typeSymbol is the
                // interface itself; the class-implemented members are intercepted via
                // RequiresExplicitImpl below instead.)
                if (typeSymbol.TypeKind == TypeKind.Class
                    && typeSymbol.FindImplementationForInterfaceMember(member) is not null)
                {
                    continue;
                }

                switch (member)
                {
                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                    {
                        var key = GetMethodKey(method);
                        if (state.SeenMethods.TryGetValue(key, out var existing))
                        {
                            // Same name+params already seen. Check if return type differs
                            // (e.g. IEnumerable<T>.GetEnumerator vs IEnumerable.GetEnumerator).
                            var fullKey = GetFullMethodKey(method);
                            if (!state.SeenFullMethods.Add(fullKey))
                            {
                                // Exact signature already seen. Normally a true duplicate — but when
                                // the prior sighting is a non-mockable class member (NonMockableEntry)
                                // and we're walking an additional interface, re-implement explicitly
                                // so the mock intercepts interface dispatch anyway.
                                if (primaryClassSymbol is null || existing.Index != NonMockableEntry.Index)
                                {
                                    // Single-interface mocks generate a wrapper that forwards each member
                                    // as an explicit interface impl, which satisfies only the slot it names.
                                    // This duplicate is a distinct slot (base member hidden by `new`, or one
                                    // inherited from multiple interfaces) — record it so the wrapper also
                                    // forwards that slot, else the build fails with CS0535 (#6252).
                                    if (primaryClassSymbol is null && existing.Index >= 0)
                                    {
                                        RecordAdditionalWrapperInterface(state.Methods, existing.Index, interfaceFqn, slotHasGetter: true, slotHasSetter: true);
                                    }
                                    continue;
                                }
                                if (!state.SeenExplicitImpls.Add($"{interfaceFqn}|{fullKey}")) continue;
                                state.Methods.Add(Tag(CreateMethodModel(method, ref state.MemberIdCounter, interfaceFqn, interfaceFqn, explicitInterfaceCanDelegate: false, compilation: compilation), ownerTypeIndex));
                                break;
                            }

                            // Signature collision with different return type → explicit interface impl.
                            // Delegation is safe only if the public method's return type implements
                            // the explicit impl's return type (e.g. IEnumerator<T> : IEnumerator).
                            var canDelegate = existing.ReturnType is not null
                                && CanDelegateReturnType(existing.ReturnType, method.ReturnType);
                            state.Methods.Add(Tag(CreateMethodModel(method, ref state.MemberIdCounter, interfaceFqn, interfaceFqn, explicitInterfaceCanDelegate: canDelegate, compilation: compilation), ownerTypeIndex));
                        }
                        else
                        {
                            var explicitName = RequiresExplicitImpl(primaryClassSymbol, method) ? interfaceFqn : null;
                            state.SeenMethods[key] = (state.Methods.Count, method.ReturnType);
                            state.SeenFullMethods.Add(GetFullMethodKey(method));
                            state.Methods.Add(Tag(CreateMethodModel(method, ref state.MemberIdCounter, explicitName, interfaceFqn, compilation: compilation), ownerTypeIndex));
                        }
                        break;
                    }

                    case IPropertySymbol property when !property.IsIndexer:
                    {
                        var key = $"P:{property.Name}";
                        if (state.SeenProperties.TryGetValue(key, out var existingIndex))
                        {
                            if (existingIndex.HasValue)
                            {
                                // Check if return type differs (e.g. IFoo.Tag:string vs IBar.Tag:int)
                                var existingProp = state.Properties[existingIndex.Value];
                                if (existingProp.ReturnType != property.Type.GetFullyQualifiedNameWithNullability())
                                {
                                    // Signature collision with different return type → explicit interface impl
                                    state.Properties.Add(Tag(CreatePropertyModel(property, ref state.MemberIdCounter, interfaceFqn, interfaceFqn, compilationAssembly, compilation), ownerTypeIndex));
                                }
                                else if (primaryClassSymbol is not null
                                    && ((!existingProp.HasGetter && property.GetMethod is not null)
                                        || (!existingProp.HasSetter && property.SetMethod is not null)))
                                {
                                    // Class-primary additional walk: the existing model may be an
                                    // override of a base virtual, and an override can't add accessors
                                    // the base lacks (CS0546) — satisfy the interface explicitly instead.
                                    if (state.SeenExplicitImpls.Add($"{interfaceFqn}|{key}"))
                                    {
                                        state.Properties.Add(Tag(CreatePropertyModel(property, ref state.MemberIdCounter, interfaceFqn, interfaceFqn, compilationAssembly, compilation), ownerTypeIndex));
                                    }
                                }
                                else if (primaryClassSymbol is null)
                                {
                                    MergePropertyAccessors(state.Properties, existingIndex.Value, property, ref state.MemberIdCounter, compilationAssembly);
                                    // Distinct slot hidden by `new` (or inherited twice) — the wrapper
                                    // needs its own explicit forward for it too (#6252), emitting only
                                    // the accessors this slot declares (#6263).
                                    RecordAdditionalWrapperInterface(state.Properties, existingIndex.Value, interfaceFqn,
                                        IsAccessorAccessible(property.GetMethod, compilationAssembly),
                                        IsAccessorAccessible(property.SetMethod, compilationAssembly));
                                }
                                // else: class-primary walk and the existing member already covers
                                // every accessor the interface needs — plain dedup.
                            }
                            else if (primaryClassSymbol is not null && state.SeenExplicitImpls.Add($"{interfaceFqn}|{key}"))
                            {
                                // Blocked by a non-mockable class member — intercept via explicit impl.
                                state.Properties.Add(Tag(CreatePropertyModel(property, ref state.MemberIdCounter, interfaceFqn, interfaceFqn, compilationAssembly, compilation), ownerTypeIndex));
                            }
                        }
                        else
                        {
                            var explicitName = RequiresExplicitImpl(primaryClassSymbol, property) ? interfaceFqn : null;
                            state.SeenProperties[key] = state.Properties.Count;
                            state.Properties.Add(Tag(CreatePropertyModel(property, ref state.MemberIdCounter, explicitName, interfaceFqn, compilationAssembly, compilation), ownerTypeIndex));
                        }
                        break;
                    }

                    case IPropertySymbol indexer when indexer.IsIndexer:
                    {
                        var paramTypes = string.Join(',', indexer.Parameters.Select(p => p.Type.GetFullyQualifiedName()));
                        var key = $"I:[{paramTypes}]";
                        if (state.SeenProperties.TryGetValue(key, out var existingIndex))
                        {
                            if (existingIndex.HasValue)
                            {
                                MergePropertyAccessors(state.Properties, existingIndex.Value, indexer, ref state.MemberIdCounter, compilationAssembly);
                                // Distinct indexer slot hidden by `new` (or inherited twice) — the
                                // wrapper needs its own explicit forward for it too (#6252), emitting
                                // only the accessors this slot declares (#6263).
                                if (primaryClassSymbol is null)
                                {
                                    RecordAdditionalWrapperInterface(state.Properties, existingIndex.Value, interfaceFqn,
                                        IsAccessorAccessible(indexer.GetMethod, compilationAssembly),
                                        IsAccessorAccessible(indexer.SetMethod, compilationAssembly));
                                }
                            }
                            else if (primaryClassSymbol is not null && state.SeenExplicitImpls.Add($"{interfaceFqn}|{key}"))
                            {
                                state.Properties.Add(Tag(CreateIndexerModel(indexer, ref state.MemberIdCounter, interfaceFqn, interfaceFqn, compilationAssembly, compilation), ownerTypeIndex));
                            }
                        }
                        else
                        {
                            var explicitName = RequiresExplicitImpl(primaryClassSymbol, indexer) ? interfaceFqn : null;
                            state.SeenProperties[key] = state.Properties.Count;
                            state.Properties.Add(Tag(CreateIndexerModel(indexer, ref state.MemberIdCounter, explicitName, interfaceFqn, compilationAssembly, compilation), ownerTypeIndex));
                        }
                        break;
                    }

                    case IEventSymbol evt:
                    {
                        var key = $"E:{evt.Name}";
                        if (!state.SeenEvents.Add(key))
                        {
                            // Distinct event slot hidden by `new` (or inherited twice) — the wrapper
                            // needs its own explicit forward for it too (#6252).
                            if (primaryClassSymbol is null)
                            {
                                RecordAdditionalWrapperInterfaceForEvent(state.Events, evt.Name, interfaceFqn);
                            }
                            continue;
                        }

                        var explicitName = RequiresExplicitImpl(primaryClassSymbol, evt) ? interfaceFqn : null;
                        state.Events.Add(Tag(CreateEventModel(evt, explicitName, interfaceFqn), ownerTypeIndex));
                        break;
                    }
                }
            }
        }
    }

    private static void ProcessClassMembers(
        ITypeSymbol typeSymbol,
        IAssemblySymbol? compilationAssembly,
        Compilation compilation,
        DiscoveryState state)
    {
        var methods = state.Methods;
        var properties = state.Properties;
        var events = state.Events;
        var seenMethods = state.SeenMethods;
        var seenFullMethods = state.SeenFullMethods;
        var seenProperties = state.SeenProperties;
        var seenEvents = state.SeenEvents;
        ref int memberIdCounter = ref state.MemberIdCounter;

        // Walk up the class hierarchy
        var current = typeSymbol;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            // Pre-compute per-level: are internal members of this type accessible?
            // Avoids repeated SymbolEqualityComparer + GivesAccessTo checks per member.
            var isExternalType = compilationAssembly is not null
                && !SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, compilationAssembly);
            var hasInternalAccess = isExternalType
                && compilationAssembly is not null
                && current.ContainingAssembly.GivesAccessTo(compilationAssembly);

            foreach (var member in current.GetMembers())
            {
                if (member.IsStatic) continue;

                // Skip private members
                if (member.DeclaredAccessibility == Accessibility.Private) continue;

                // Sealed overrides can't be re-overridden, but must still be registered
                // in the seen sets to block the base virtual from being collected.
                if (member is IMethodSymbol { IsSealed: true } sealedMethod)
                {
                    var sealedKey = GetMethodKey(sealedMethod);
                    seenMethods.TryAdd(sealedKey, NonMockableEntry);
                    seenFullMethods.Add(GetFullMethodKey(sealedMethod));
                    continue;
                }
                if (member is IPropertySymbol { IsSealed: true } sealedProp)
                {
                    seenProperties.TryAdd($"P:{sealedProp.Name}", null);
                    continue;
                }

                // Skip members inaccessible from the compilation assembly
                // (e.g., internal virtual methods from external assemblies like Azure SDK)
                if (isExternalType && !IsMemberAccessibleFromExternal(member, compilationAssembly!, hasInternalAccess)) continue;

                // Non-virtual members are recorded in the seen-sets (but not collected) so that
                // base virtuals hidden by 'new' in a derived class are not emitted as overrides.
                switch (member)
                {
                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                    {
                        var key = GetMethodKey(method);
                        // Seed both seen sets so the interface loop doesn't re-add class members
                        seenFullMethods.Add(GetFullMethodKey(method));
                        // ref / ref readonly returns can't be routed through the mock engine —
                        // treat them as non-mockable so the inherited impl flows through unchanged.
                        if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                        {
                            seenMethods.TryAdd(key, NonMockableEntry);
                            break;
                        }
                        if (method.IsAbstract || method.IsVirtual || method.IsOverride)
                        {
                            if (seenMethods.ContainsKey(key)) continue;
                            seenMethods[key] = (methods.Count, method.ReturnType);
                            methods.Add(CreateMethodModel(method, ref memberIdCounter, null, compilationAssembly: compilationAssembly, compilation: compilation));
                        }
                        else
                        {
                            seenMethods.TryAdd(key, NonMockableEntry);
                        }
                        break;
                    }

                    case IPropertySymbol property when !property.IsIndexer:
                    {
                        var key = $"P:{property.Name}";
                        if (property.IsAbstract || property.IsVirtual || property.IsOverride)
                        {
                            if (seenProperties.TryGetValue(key, out var existingIndex))
                            {
                                if (existingIndex.HasValue)
                                {
                                    MergePropertyAccessors(properties, existingIndex.Value, property, ref memberIdCounter, compilationAssembly);
                                }
                            }
                            else
                            {
                                seenProperties[key] = properties.Count;
                                properties.Add(CreatePropertyModel(property, ref memberIdCounter, null, compilationAssembly: compilationAssembly, compilation: compilation));
                            }
                        }
                        else if (!seenProperties.ContainsKey(key))
                        {
                            seenProperties[key] = null;
                        }
                        break;
                    }

                    case IPropertySymbol indexer when indexer.IsIndexer:
                    {
                        var paramTypes = string.Join(',', indexer.Parameters.Select(p => p.Type.GetFullyQualifiedName()));
                        var key = $"I:[{paramTypes}]";
                        if (indexer.IsAbstract || indexer.IsVirtual || indexer.IsOverride)
                        {
                            if (seenProperties.TryGetValue(key, out var existingIndex))
                            {
                                if (existingIndex.HasValue)
                                {
                                    MergePropertyAccessors(properties, existingIndex.Value, indexer, ref memberIdCounter, compilationAssembly);
                                }
                            }
                            else
                            {
                                seenProperties[key] = properties.Count;
                                properties.Add(CreateIndexerModel(indexer, ref memberIdCounter, null, compilationAssembly: compilationAssembly, compilation: compilation));
                            }
                        }
                        else if (!seenProperties.ContainsKey(key))
                        {
                            seenProperties[key] = null;
                        }
                        break;
                    }

                    case IEventSymbol evt:
                    {
                        var key = $"E:{evt.Name}";
                        if (evt.IsAbstract || evt.IsVirtual || evt.IsOverride)
                        {
                            if (!seenEvents.Add(key)) continue;
                            events.Add(CreateEventModel(evt, null, compilationAssembly: compilationAssembly));
                        }
                        else
                        {
                            seenEvents.Add(key);
                        }
                        break;
                    }
                }
            }

            current = current.BaseType;
        }
    }

    /// <summary>
    /// Checks accessibility for a member known to be from an external assembly.
    /// Used in the hot loop where assembly identity is pre-computed per hierarchy level.
    /// </summary>
    private static bool IsMemberAccessibleFromExternal(ISymbol member, IAssemblySymbol compilationAssembly, bool hasInternalAccess)
    {
        var accessibility = member.DeclaredAccessibility;
        // Private: never accessible from another assembly (no InternalsVisibleTo equivalent for private).
        // ProtectedOrInternal (protected internal) is intentionally NOT blocked here:
        // the generated mock subclasses the target, so the protected part grants access.
        // ProtectedAndInternal (private protected) requires BOTH inheritance AND internal access.
        if (accessibility == Accessibility.Private)
            return false;
        if (accessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal)
        {
            if (!hasInternalAccess)
                return false;
        }

        return AreMemberSignatureTypesAccessible(member, compilationAssembly);
    }

    /// <summary>
    /// Full accessibility check for members where the containing assembly isn't pre-computed
    /// (used by DiscoverConstructors and IsAccessorAccessible).
    /// </summary>
    private static bool IsMemberAccessible(ISymbol member, IAssemblySymbol? compilationAssembly)
    {
        if (compilationAssembly is null) return true;

        var memberAssembly = member.ContainingAssembly;
        if (SymbolEqualityComparer.Default.Equals(memberAssembly, compilationAssembly))
            return true;

        var accessibility = member.DeclaredAccessibility;
        // Private: never reachable cross-assembly (no InternalsVisibleTo equivalent for private).
        if (accessibility == Accessibility.Private)
            return false;
        if (accessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal)
        {
            if (!memberAssembly.GivesAccessTo(compilationAssembly))
                return false;
        }

        return AreMemberSignatureTypesAccessible(member, compilationAssembly);
    }

    private static bool AreMemberSignatureTypesAccessible(ISymbol member, IAssemblySymbol compilationAssembly)
    {
        switch (member)
        {
            case IMethodSymbol method:
                if (!IsTypeAccessible(method.ReturnType, compilationAssembly)) return false;
                foreach (var param in method.Parameters)
                {
                    if (!IsTypeAccessible(param.Type, compilationAssembly)) return false;
                }
                return true;

            case IPropertySymbol property:
                return IsTypeAccessible(property.Type, compilationAssembly);

            case IEventSymbol evt:
                return IsTypeAccessible(evt.Type, compilationAssembly);

            default:
                return true;
        }
    }

    private static bool IsTypeAccessible(ITypeSymbol type, IAssemblySymbol compilationAssembly)
    {
        // Type parameters are always accessible
        if (type is ITypeParameterSymbol) return true;

        // Pointer types can't appear in mock override signatures (even same-assembly)
        if (type is IPointerTypeSymbol or IFunctionPointerTypeSymbol) return false;

        // Arrays: check element type
        if (type is IArrayTypeSymbol arrayType)
            return IsTypeAccessible(arrayType.ElementType, compilationAssembly);

        // Check the type itself before recursing into type arguments (short-circuits early)
        var typeAssembly = type.ContainingAssembly;
        if (typeAssembly is not null
            && !SymbolEqualityComparer.Default.Equals(typeAssembly, compilationAssembly))
        {
            var accessibility = type.DeclaredAccessibility;
            if (accessibility == Accessibility.Private) return false;
            if (accessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal)
            {
                if (!typeAssembly.GivesAccessTo(compilationAssembly)) return false;
            }
        }

        // For generic types, also check type arguments
        if (type is INamedTypeSymbol namedType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                if (!IsTypeAccessible(typeArg, compilationAssembly)) return false;
            }
        }

        return true;
    }

    private static string GetOverrideAccessModifier(ISymbol member, IAssemblySymbol? compilationAssembly)
        => member.DeclaredAccessibility switch
        {
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.ProtectedOrInternal => HasInternalAccess(member, compilationAssembly) ? "protected internal" : "protected",
            _ => "public"
        };

    private static bool HasInternalAccess(ISymbol member, IAssemblySymbol? compilationAssembly)
    {
        if (compilationAssembly is null)
        {
            return true;
        }

        var memberAssembly = member.ContainingAssembly;
        return memberAssembly is null
            || SymbolEqualityComparer.Default.Equals(memberAssembly, compilationAssembly)
            || memberAssembly.GivesAccessTo(compilationAssembly);
    }

    private static string GetAccessorAccessModifier(
        IMethodSymbol? accessor,
        string propertyAccessModifier,
        IAssemblySymbol? compilationAssembly)
    {
        if (accessor is null)
        {
            return "";
        }

        var accessorAccessModifier = GetOverrideAccessModifier(accessor, compilationAssembly);
        return accessorAccessModifier == propertyAccessModifier ? "" : accessorAccessModifier;
    }

    /// <summary>
    /// Creates a MockMemberModel from a delegate type's Invoke method.
    /// </summary>
    public static MockMemberModel CreateDelegateInvokeModel(IMethodSymbol invokeMethod, ref int memberIdCounter, Compilation compilation)
    {
        return CreateMethodModel(invokeMethod, ref memberIdCounter, null, compilation: compilation);
    }

    private static MockMemberModel CreateMethodModel(IMethodSymbol method, ref int memberIdCounter, string? explicitInterfaceName, string? declaringInterfaceName = null, bool explicitInterfaceCanDelegate = false, IAssemblySymbol? compilationAssembly = null, Compilation compilation = null!)
    {
        var returnType = method.ReturnType;
        var isAsync = returnType.IsAsyncReturnType();
        var isValueTask = returnType.IsValueTaskReturnType();
        var (unwrappedType, isVoidAsync) = returnType.GetUnwrappedReturnType();
        var isVoid = method.ReturnsVoid || isVoidAsync;

        // Check if the effective return type (unwrapped for async) is an interface with
        // static abstract members. Such types cannot be used as generic type arguments (CS8920).
        var effectiveReturnTypeSymbol = returnType.GetAsyncInnerTypeSymbol() ?? returnType;
        var returnTypeHasStaticAbstract = !isVoid && IsInterfaceWithStaticAbstractMembers(effectiveReturnTypeSymbol);
        var autoMockFactoryMethod = !isVoid
            ? GetAutoMockFactoryMethod(effectiveReturnTypeSymbol, compilation)
            : null;

        return new MockMemberModel
        {
            Name = method.Name,
            MemberId = memberIdCounter++,
            ReturnType = returnType.GetFullyQualifiedNameWithNullability(),
            UnwrappedReturnType = unwrappedType,
            IsVoid = isVoid,
            IsAsync = isAsync,
            IsValueTask = isValueTask,
            IsProperty = false,
            IsGenericMethod = method.IsGenericMethod,
            Parameters = new EquatableArray<MockParameterModel>(
                method.Parameters.Select(p => new MockParameterModel
                {
                    Name = EscapeIdentifier(p.Name),
                    Type = p.Type.GetMinimallyQualifiedNameWithNullability(),
                    FullyQualifiedType = p.Type.GetFullyQualifiedNameWithNullability(),
                    Direction = p.GetParameterDirection(),
                    HasDefaultValue = p.HasExplicitDefaultValue,
                    DefaultValueExpression = p.HasExplicitDefaultValue ? FormatDefaultValue(p) : null,
                    IsValueType = p.Type.IsValueType,
                    IsRefStruct = p.Type.IsRefLikeType,
                    SpanElementType = GetSpanElementType(p.Type),
                    // Only single-dimensional arrays get the params-expanded setup overload;
                    // params collections (C# 13) and params spans degrade to whole-value matching.
                    ParamsElementType = p.IsParams && p.Type is IArrayTypeSymbol paramsArray
                        ? paramsArray.ElementType.GetFullyQualifiedNameWithNullability()
                        : null
                }).ToImmutableArray()
            ),
            TypeParameters = new EquatableArray<MockTypeParameterModel>(
                method.TypeParameters.Select(tp => new MockTypeParameterModel
                {
                    Name = tp.Name,
                    Constraints = tp.GetGenericConstraints(),
                    HasReferenceTypeConstraint = tp.HasReferenceTypeConstraint,
                    HasValueTypeConstraint = tp.HasValueTypeConstraint,
                    HasAnnotatedNullableUsage = tp.NeedsDefaultConstraintForNullableUsage(method)
                }).ToImmutableArray()
            ),
            ExplicitInterfaceName = explicitInterfaceName,
            ExplicitInterfaceCanDelegate = explicitInterfaceCanDelegate,
            DeclaringInterfaceName = declaringInterfaceName,
            NullableAnnotation = returnType.NullableAnnotation.ToString(),
            SmartDefault = isVoid ? "" : returnType.GetSmartDefault(returnType.IsNullableAnnotated()),
            UnwrappedSmartDefault = ComputeUnwrappedSmartDefault(returnType, isVoid, isAsync),
            IsAbstractMember = method.IsAbstract,
            IsVirtualMember = method.IsVirtual || method.IsOverride,
            OverrideAccessModifier = GetOverrideAccessModifier(method, compilationAssembly),
            IsRefStructReturn = returnType.IsRefLikeType,
            AutoMockFactoryMethod = autoMockFactoryMethod,
            IsReturnTypeStaticAbstractInterface = returnTypeHasStaticAbstract,
            SpanReturnElementType = returnType.IsRefLikeType ? GetSpanElementType(returnType) : null,
            ObsoleteAttribute = GetObsoleteAttributeSyntax(method)
        };
    }

    /// <summary>
    /// When a property with the same name appears from multiple interfaces, merge getter/setter
    /// accessors so the generated class satisfies all interfaces.
    /// </summary>
    private static void MergePropertyAccessors(List<MockMemberModel> properties, int existingIndex,
        IPropertySymbol newProperty, ref int memberIdCounter, IAssemblySymbol? compilationAssembly = null)
    {
        var existing = properties[existingIndex];
        var newGetterAccessible = IsAccessorAccessible(newProperty.GetMethod, compilationAssembly);
        var newSetterAccessible = IsAccessorAccessible(newProperty.SetMethod, compilationAssembly);
        var needsGetter = !existing.HasGetter && newGetterAccessible;
        var needsSetter = !existing.HasSetter && newSetterAccessible;

        if (!needsGetter && !needsSetter) return;

        properties[existingIndex] = existing with
        {
            HasGetter = existing.HasGetter || newGetterAccessible,
            HasSetter = existing.HasSetter || newSetterAccessible,
            SetterMemberId = existing.HasSetter ? existing.SetterMemberId
                : newSetterAccessible ? memberIdCounter++ : existing.SetterMemberId
        };
    }

    /// <summary>
    /// Returns true if the accessor exists AND is accessible from the compilation assembly.
    /// Needed because e.g. `internal set` on an external type exists in the symbol but can't be overridden.
    /// </summary>
    private static bool IsAccessorAccessible(IMethodSymbol? accessor, IAssemblySymbol? compilationAssembly)
        => accessor is not null && IsMemberAccessible(accessor, compilationAssembly);

    private static MockMemberModel CreatePropertyModel(IPropertySymbol property, ref int memberIdCounter, string? explicitInterfaceName, string? declaringInterfaceName = null, IAssemblySymbol? compilationAssembly = null, Compilation compilation = null!)
    {
        var hasGetter = IsAccessorAccessible(property.GetMethod, compilationAssembly);
        var hasSetter = IsAccessorAccessible(property.SetMethod, compilationAssembly);
        var getterId = memberIdCounter++;
        var setterId = hasSetter ? memberIdCounter++ : 0;
        var propertyObsolete = GetObsoleteAttributeSyntax(property);
        var overrideAccessModifier = GetOverrideAccessModifier(property, compilationAssembly);

        return new MockMemberModel
        {
            Name = property.Name,
            MemberId = getterId,
            ReturnType = property.Type.GetFullyQualifiedNameWithNullability(),
            UnwrappedReturnType = property.Type.GetFullyQualifiedNameWithNullability(),
            IsVoid = false,
            IsAsync = false,
            IsProperty = true,
            HasGetter = hasGetter,
            HasSetter = hasSetter,
            OwnHasGetter = hasGetter,
            OwnHasSetter = hasSetter,
            SetterMemberId = setterId,
            ExplicitInterfaceName = explicitInterfaceName,
            DeclaringInterfaceName = declaringInterfaceName,
            NullableAnnotation = property.Type.NullableAnnotation.ToString(),
            SmartDefault = property.Type.GetSmartDefault(property.Type.IsNullableAnnotated()),
            IsAbstractMember = property.IsAbstract,
            IsVirtualMember = property.IsVirtual || property.IsOverride,
            OverrideAccessModifier = overrideAccessModifier,
            GetterAccessModifier = GetAccessorAccessModifier(property.GetMethod, overrideAccessModifier, compilationAssembly),
            SetterAccessModifier = GetAccessorAccessModifier(property.SetMethod, overrideAccessModifier, compilationAssembly),
            IsRefStructReturn = property.Type.IsRefLikeType,
            AutoMockFactoryMethod = GetAutoMockFactoryMethod(property.Type, compilation),
            IsReturnTypeStaticAbstractInterface = IsInterfaceWithStaticAbstractMembers(property.Type),
            SpanReturnElementType = property.Type.IsRefLikeType ? GetSpanElementType(property.Type) : null,
            ObsoleteAttribute = propertyObsolete,
            GetterObsoleteAttribute = GetAccessorObsoleteAttributeSyntax(propertyObsolete, property.GetMethod),
            SetterObsoleteAttribute = GetAccessorObsoleteAttributeSyntax(propertyObsolete, property.SetMethod)
        };
    }

    /// <summary>Returns the [Obsolete] attribute for a single accessor, but only when the
    /// containing property is NOT itself marked obsolete. When the property is marked, the
    /// property-level emission already covers the accessor and emitting both would duplicate.
    /// Takes the already-computed property-level attribute string to avoid re-iterating
    /// <c>property.GetAttributes()</c> for each accessor.</summary>
    private static string GetAccessorObsoleteAttributeSyntax(string propertyObsoleteAttribute, IMethodSymbol? accessor)
    {
        if (accessor is null)
        {
            return "";
        }
        if (propertyObsoleteAttribute.Length > 0)
        {
            return "";
        }
        return GetObsoleteAttributeSyntax(accessor);
    }

    /// <summary>
    /// Discovers all accessible constructors of a class type for partial mock generation.
    /// </summary>
    public static EquatableArray<MockConstructorModel> DiscoverConstructors(INamedTypeSymbol typeSymbol, IAssemblySymbol? compilationAssembly = null)
    {
        var constructors = new List<MockConstructorModel>();

        foreach (var ctor in typeSymbol.InstanceConstructors)
        {
            if (ctor.DeclaredAccessibility == Accessibility.Private) continue;
            if (!IsMemberAccessible(ctor, compilationAssembly)) continue;

            constructors.Add(new MockConstructorModel
            {
                Parameters = new EquatableArray<MockParameterModel>(
                    ctor.Parameters.Select(p => new MockParameterModel
                    {
                        Name = EscapeIdentifier(p.Name),
                        Type = p.Type.GetMinimallyQualifiedNameWithNullability(),
                        FullyQualifiedType = p.Type.GetFullyQualifiedNameWithNullability(),
                        Direction = p.GetParameterDirection(),
                        HasDefaultValue = p.HasExplicitDefaultValue,
                        DefaultValueExpression = p.HasExplicitDefaultValue ? FormatDefaultValue(p) : null,
                        IsValueType = p.Type.IsValueType,
                        IsRefStruct = p.Type.IsRefLikeType
                    }).ToImmutableArray()
                )
            });
        }

        return new EquatableArray<MockConstructorModel>(constructors.ToImmutableArray());
    }

    private static MockMemberModel CreateIndexerModel(IPropertySymbol indexer, ref int memberIdCounter, string? explicitInterfaceName, string? declaringInterfaceName = null, IAssemblySymbol? compilationAssembly = null, Compilation compilation = null!)
    {
        var hasGetter = IsAccessorAccessible(indexer.GetMethod, compilationAssembly);
        var hasSetter = IsAccessorAccessible(indexer.SetMethod, compilationAssembly);
        var getterId = memberIdCounter++;
        var setterId = hasSetter ? memberIdCounter++ : 0;
        var indexerObsolete = GetObsoleteAttributeSyntax(indexer);
        var overrideAccessModifier = GetOverrideAccessModifier(indexer, compilationAssembly);

        return new MockMemberModel
        {
            Name = "this",
            MemberId = getterId,
            SetterMemberId = setterId,
            ReturnType = indexer.Type.GetFullyQualifiedNameWithNullability(),
            UnwrappedReturnType = indexer.Type.GetFullyQualifiedNameWithNullability(),
            IsVoid = false,
            IsAsync = false,
            IsProperty = true,
            IsIndexer = true,
            HasGetter = hasGetter,
            HasSetter = hasSetter,
            OwnHasGetter = hasGetter,
            OwnHasSetter = hasSetter,
            Parameters = new EquatableArray<MockParameterModel>(
                indexer.Parameters.Select(p => new MockParameterModel
                {
                    Name = EscapeIdentifier(p.Name),
                    Type = p.Type.GetMinimallyQualifiedNameWithNullability(),
                    FullyQualifiedType = p.Type.GetFullyQualifiedNameWithNullability(),
                    Direction = p.GetParameterDirection()
                }).ToImmutableArray()
            ),
            ExplicitInterfaceName = explicitInterfaceName,
            DeclaringInterfaceName = declaringInterfaceName,
            NullableAnnotation = indexer.Type.NullableAnnotation.ToString(),
            SmartDefault = indexer.Type.GetSmartDefault(indexer.Type.IsNullableAnnotated()),
            OverrideAccessModifier = overrideAccessModifier,
            GetterAccessModifier = GetAccessorAccessModifier(indexer.GetMethod, overrideAccessModifier, compilationAssembly),
            SetterAccessModifier = GetAccessorAccessModifier(indexer.SetMethod, overrideAccessModifier, compilationAssembly),
            IsRefStructReturn = indexer.Type.IsRefLikeType,
            AutoMockFactoryMethod = GetAutoMockFactoryMethod(indexer.Type, compilation),
            IsReturnTypeStaticAbstractInterface = IsInterfaceWithStaticAbstractMembers(indexer.Type),
            SpanReturnElementType = indexer.Type.IsRefLikeType ? GetSpanElementType(indexer.Type) : null,
            ObsoleteAttribute = indexerObsolete,
            GetterObsoleteAttribute = GetAccessorObsoleteAttributeSyntax(indexerObsolete, indexer.GetMethod),
            SetterObsoleteAttribute = GetAccessorObsoleteAttributeSyntax(indexerObsolete, indexer.SetMethod)
        };
    }

    private static string? GetAutoMockFactoryMethod(ITypeSymbol returnType, Compilation compilation)
    {
        var effectiveReturnType = returnType.GetAsyncInnerTypeSymbol() ?? returnType;
        if (effectiveReturnType is not INamedTypeSymbol namedType)
            return null;

        if (namedType.TypeKind != TypeKind.Interface)
            return null;

        var namespaceName = namedType.ContainingNamespace?.ToDisplayString() ?? "";
        if (namespaceName == "System" || namespaceName.StartsWith("System.")
            || namespaceName == "Microsoft" || namespaceName.StartsWith("Microsoft.")
            || namespaceName == "Windows" || namespaceName.StartsWith("Windows."))
        {
            return null;
        }

        if (IsInterfaceWithStaticAbstractMembers(effectiveReturnType))
            return null;

        var baseName = namedType.OriginalDefinition.GetGeneratedMockBaseName();
        var factoryNamespace = namedType.OriginalDefinition.GetGeneratedMockNamespace(compilation);
        var globalPrefix = Builders.MockImplBuilder.ToGlobalPrefix(factoryNamespace);

        if (!namedType.IsGenericType)
        {
            return $"{globalPrefix}{baseName}MockFactory.CreateAutoMock";
        }

        var typeArguments = string.Join(", ", namedType.TypeArguments.Select(x => x.GetFullyQualifiedNameWithNullability()));
        return $"{globalPrefix}{baseName}MockFactory.CreateAutoMock<{typeArguments}>";
    }

    private static MockEventModel CreateEventModel(IEventSymbol evt, string? explicitInterfaceName, string? declaringInterfaceName = null, IAssemblySymbol? compilationAssembly = null)
    {
        var eventHandlerType = evt.Type.GetFullyQualifiedNameWithNullability();

        // Determine if this is an EventHandler pattern (sender + args)
        var isEventHandlerPattern = IsEventHandlerType(evt.Type);

        // Get the delegate's Invoke method to inspect parameters
        var invokeMethod = (evt.Type as INamedTypeSymbol)?.DelegateInvokeMethod;

        string raiseParameters;
        string invokeArgs;
        string eventArgsType;
        IParameterSymbol[] raiseParams;

        if (invokeMethod is null || invokeMethod.Parameters.Length == 0)
        {
            // Parameterless delegate (e.g., Action)
            raiseParameters = "";
            invokeArgs = "";
            eventArgsType = "";
            raiseParams = [];
        }
        else if (isEventHandlerPattern && invokeMethod.Parameters.Length >= 2)
        {
            // EventHandler pattern: skip sender (first param), expose remaining as raise params
            var argsParams = invokeMethod.Parameters.Skip(1).ToArray();
            raiseParameters = string.Join(", ", argsParams.Select(p => $"{p.Type.GetFullyQualifiedName()} {EscapeIdentifier(p.Name)}"));
            invokeArgs = "this, " + string.Join(", ", argsParams.Select(p => EscapeIdentifier(p.Name)));
            eventArgsType = argsParams.Length == 1
                ? argsParams[0].Type.GetFullyQualifiedName()
                : raiseParameters; // fallback for multi-arg EventHandler subtypes
            raiseParams = argsParams;
        }
        else
        {
            // Custom delegate (Action<T>, Func<T>, user-defined): expose all params
            raiseParameters = string.Join(", ", invokeMethod.Parameters.Select(p => $"{p.Type.GetFullyQualifiedName()} {EscapeIdentifier(p.Name)}"));
            invokeArgs = string.Join(", ", invokeMethod.Parameters.Select(p => EscapeIdentifier(p.Name)));
            eventArgsType = raiseParameters;
            raiseParams = invokeMethod.Parameters.ToArray();
        }

        var raiseParameterList = new EquatableArray<MockParameterModel>(
            raiseParams.Select(p => new MockParameterModel
            {
                Name = EscapeIdentifier(p.Name),
                FullyQualifiedType = p.Type.GetFullyQualifiedNameWithNullability(),
                Type = p.Type.GetMinimallyQualifiedNameWithNullability(),
                Direction = ParameterDirection.In
            }).ToImmutableArray());

        return new MockEventModel
        {
            Name = evt.Name,
            EventHandlerType = eventHandlerType,
            InvokeArgs = invokeArgs,
            EventArgsType = eventArgsType,
            ExplicitInterfaceName = explicitInterfaceName,
            DeclaringInterfaceName = declaringInterfaceName,
            OverrideAccessModifier = GetOverrideAccessModifier(evt, compilationAssembly),
            RaiseParameterList = raiseParameterList,
            ObsoleteAttribute = GetObsoleteAttributeSyntax(evt)
        };
    }

    private static bool IsEventHandlerType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // Check if the type is System.EventHandler or System.EventHandler<T>
        var fullName = namedType.ConstructedFrom.ToDisplayString();
        return fullName == "System.EventHandler" || fullName == "System.EventHandler<TEventArgs>";
    }

    private static string ComputeUnwrappedSmartDefault(ITypeSymbol returnType, bool isVoid, bool isAsync)
    {
        if (isVoid) return "";
        if (!isAsync) return returnType.GetSmartDefault(returnType.IsNullableAnnotated());

        // For async generic types (Task<T>/ValueTask<T>), get the inner type's smart default
        var innerType = returnType.GetAsyncInnerTypeSymbol();
        if (innerType is not null)
        {
            return innerType.GetSmartDefault(innerType.IsNullableAnnotated());
        }

        // Fallback (shouldn't happen for async non-void)
        return "default!";
    }

    private static string GetMethodKey(IMethodSymbol method)
    {
        var paramTypes = string.Join(',', method.Parameters.Select(p =>
            p.Type.GetFullyQualifiedName() + (p.RefKind != RefKind.None ? "&" : "")));
        var typeParams = method.TypeParameters.Length > 0 ? $"`{method.TypeParameters.Length}" : "";
        return $"M:{method.Name}{typeParams}({paramTypes})";
    }

    /// <summary>
    /// Full method key INCLUDING return type — used to distinguish interface methods that share
    /// name+params but differ in return type (e.g. IEnumerable&lt;T&gt;.GetEnumerator vs
    /// IEnumerable.GetEnumerator). The second one needs explicit interface implementation.
    /// </summary>
    private static string GetFullMethodKey(IMethodSymbol method)
    {
        return $"{GetMethodKey(method)}:{method.ReturnType.GetFullyQualifiedName()}";
    }

    /// <summary>
    /// Returns true if <paramref name="publicReturnType"/> is assignable to
    /// <paramref name="explicitReturnType"/>, meaning the explicit impl can safely
    /// delegate to the public method (e.g. IEnumerator&lt;T&gt; → IEnumerator).
    /// </summary>
    private static bool CanDelegateReturnType(ITypeSymbol publicReturnType, ITypeSymbol explicitReturnType)
    {
        if (explicitReturnType.TypeKind != TypeKind.Interface) return false;
        if (SymbolEqualityComparer.Default.Equals(publicReturnType, explicitReturnType)) return true;
        return publicReturnType.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, explicitReturnType.OriginalDefinition));
    }

    private static string? FormatDefaultValue(IParameterSymbol param)
    {
        if (!param.HasExplicitDefaultValue) return null;
        var value = param.ExplicitDefaultValue;
        if (value is null) return "default";
        if (value is string s) return $"\"{s}\"";
        if (value is bool b) return b ? "true" : "false";
        if (value is char c) return $"'{c}'";
        return value.ToString();
    }

    /// <summary>
    /// If <paramref name="symbol"/> carries <see cref="System.ObsoleteAttribute"/>, returns
    /// the C# attribute syntax to copy onto a generated forward/override (preserving the
    /// message, IsError flag, and the C# 10+ <c>DiagnosticId</c> / <c>UrlFormat</c> named
    /// arguments). Returns empty string when the member is not obsolete. Suppresses
    /// CS0612/CS0618 inside the generated body and resolves CS0672 on overrides.
    /// </summary>
    private static string GetObsoleteAttributeSyntax(ISymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null)
            {
                continue;
            }
            if (!string.Equals(attrClass.Name, "ObsoleteAttribute", StringComparison.Ordinal))
            {
                continue;
            }
            if (!string.Equals(attrClass.ContainingNamespace?.ToDisplayString(), "System", StringComparison.Ordinal))
            {
                continue;
            }

            var positional = new List<string>();
            var args = attr.ConstructorArguments;
            if (args.Length >= 1)
            {
                var message = args[0].Value as string;
                positional.Add(message is null ? "null" : EscapeStringLiteral(message));
            }
            if (args.Length >= 2)
            {
                var isError = args[1].Value is bool b && b;
                positional.Add(isError ? "true" : "false");
            }

            // C# 10+ named arguments. DiagnosticId lets consumers suppress the generated
            // obsolete warning using their custom ID instead of CS0618. UrlFormat surfaces
            // a documentation link in IDE tooltips.
            //
            // Only the string-valued named arguments DiagnosticId and UrlFormat are preserved.
            // System.ObsoleteAttribute defines no others today. If .NET adds new ones, the
            // generated attribute will still compile but won't carry the extra metadata.
            // Extend this loop when a new named arg is added.
            var named = new List<string>();
            foreach (var na in attr.NamedArguments)
            {
                if (na.Value.Value is not string strValue)
                {
                    continue;
                }
                if (string.Equals(na.Key, "DiagnosticId", StringComparison.Ordinal)
                 || string.Equals(na.Key, "UrlFormat", StringComparison.Ordinal))
                {
                    named.Add($"{na.Key} = {EscapeStringLiteral(strValue)}");
                }
            }

            if (positional.Count == 0 && named.Count == 0)
            {
                return "[global::System.Obsolete]";
            }
            var allArgs = string.Join(", ", positional.Concat(named));
            return $"[global::System.Obsolete({allArgs})]";
        }
        return "";
    }

    /// <summary>Wraps a string in C# double-quoted literal syntax, escaping backslashes,
    /// quotes, and the newline/carriage-return/tab whitespace escapes that would otherwise
    /// produce a syntactically invalid multi-line string literal.</summary>
    private static string EscapeStringLiteral(string value)
        => "\"" + value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t")
            + "\"";

    /// <summary>
    /// For ReadOnlySpan&lt;T&gt; or Span&lt;T&gt; types, returns the fully qualified element type.
    /// Returns null for all other types.
    /// </summary>
    private static string? GetSpanElementType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } namedType)
        {
            return null;
        }

        var constructed = namedType.ConstructedFrom;
        var ns = constructed.ContainingNamespace?.ToDisplayString();
        var name = constructed.MetadataName;

        if (ns == "System" && name is "ReadOnlySpan`1" or "Span`1")
        {
            return namedType.TypeArguments[0].GetFullyQualifiedName();
        }

        return null;
    }

    /// <summary>
    /// Internal predicate consumed only by <see cref="TryCollectStaticAbstractFromInterface"/>.
    /// Class targets already provide the concrete static impl that satisfies any static-abstract
    /// interface members; emitting a bridge interface for them would produce CS0527 (class in
    /// interface list) and CS0540 (explicit interface impl on a type that doesn't list the
    /// interface). Centralised here so no caller can bypass the gate by accident.
    /// </summary>
    private static bool ShouldCollectStaticAbstractFromInterfaces(ITypeSymbol typeSymbol)
        => typeSymbol.TypeKind == TypeKind.Interface;

    /// <summary>
    /// Gated entry point used by every interface-member discovery loop for static members.
    /// Derives the static-abstract collection flag from <paramref name="typeSymbol"/> internally
    /// so that adding a future loop cannot silently re-introduce a class-target regression
    /// (CS0527 / CS0540 from a class being treated like an interface) — the only way to collect
    /// a static-abstract member is through this helper.
    /// </summary>
    private static void TryCollectStaticAbstractFromInterface(
        ISymbol member,
        ITypeSymbol typeSymbol,
        string interfaceFqn,
        DiscoveryState state,
        Compilation compilation)
    {
        if (!member.IsAbstract) return;
        if (!ShouldCollectStaticAbstractFromInterfaces(typeSymbol)) return;

        CollectStaticAbstractMember(member, interfaceFqn, state, compilation);
    }

    /// <summary>
    /// Returns true when the given type symbol is an interface that contains static abstract members
    /// (directly or via inherited interfaces) without a most specific implementation.
    /// Such types cannot be used as generic type arguments (CS8920).
    /// </summary>
    private static bool IsInterfaceWithStaticAbstractMembers(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Interface } namedType)
        {
            return false;
        }

        // Check both direct members and inherited interface members.
        // AllInterfaces does NOT include the type itself, so we check both.
        return namedType.GetMembers()
            .Concat(namedType.AllInterfaces.SelectMany(i => i.GetMembers()))
            .Any(m => m.IsStatic && m.IsAbstract);
    }

    /// <summary>
    /// Collects a static abstract interface member as a full MockMemberModel with IsStaticAbstract=true.
    /// Uses the existing CreateMethodModel/CreatePropertyModel to generate models with MemberIds,
    /// enabling full setup/verify support through the mock engine.
    /// </summary>
    private static void CollectStaticAbstractMember(
        ISymbol member,
        string interfaceFqn,
        DiscoveryState state,
        Compilation compilation)
    {
        var methods = state.Methods;
        var properties = state.Properties;
        var events = state.Events;
        var seenMethods = state.SeenMethods;
        var seenProperties = state.SeenProperties;
        var seenEvents = state.SeenEvents;
        ref int memberIdCounter = ref state.MemberIdCounter;

        switch (member)
        {
            case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
            {
                var key = GetMethodKey(method);
                if (seenMethods.ContainsKey(key)) break;
                seenMethods[key] = (methods.Count, method.ReturnType);

                var model = CreateMethodModel(method, ref memberIdCounter, interfaceFqn, compilation: compilation) with
                {
                    IsStaticAbstract = true
                };
                methods.Add(model);
                break;
            }

            case IPropertySymbol property when !property.IsIndexer:
            {
                var key = $"P:{property.Name}";
                if (seenProperties.ContainsKey(key)) break;

                seenProperties[key] = properties.Count;
                var model = CreatePropertyModel(property, ref memberIdCounter, interfaceFqn, compilation: compilation) with
                {
                    IsStaticAbstract = true
                };
                properties.Add(model);
                break;
            }

            case IEventSymbol evt:
            {
                var key = $"E:{evt.Name}";
                if (!seenEvents.Add(key)) break;

                var model = CreateEventModel(evt, interfaceFqn) with
                {
                    IsStaticAbstract = true
                };
                events.Add(model);
                break;
            }
        }
    }
}
