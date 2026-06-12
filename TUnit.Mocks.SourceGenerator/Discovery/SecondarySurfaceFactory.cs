using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TUnit.Mocks.SourceGenerator.Builders;
using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Discovery;

/// <summary>
/// Builds the per-(primary, additional-interface) PAIR artifacts for multi-type mocks:
///
/// 1. <see cref="ComputeMemberIdMap"/> — the standalone→union member-ID translation a combo's
///    factory registers on its engine. The setup extensions for an additional interface are
///    generated ONCE per (T1, Tn) pair from Tn's standalone walk (so they are identical no matter
///    how many combos contain the pair and dedupe in the pipeline), which means they carry Tn's
///    standalone member IDs — but each combo's impl dispatches its own union IDs, and the same
///    member can have a different union ID in Mock.Of&lt;T1,T2&gt; vs Mock.Of&lt;T1,T2,T3&gt;.
///    The map bridges the two at runtime.
///
/// 2. <see cref="BuildPairModel"/> — the pair model the members builder turns into that shared
///    extension surface (targeting Mock&lt;T1&gt;, member IDs = standalone ordinals).
/// </summary>
internal static class SecondarySurfaceFactory
{
    /// <summary>
    /// Maps every member ID of <paramref name="standalone"/> (an additional interface's
    /// standalone model) to the matching member's ID in <paramref name="union"/> (the combo's
    /// multi-type model), correlating by full signature. Unmatched slots get -1.
    /// Members the union deduplicated against the primary map to the PRIMARY member's ID,
    /// so a setup made through either surface configures the same engine slot.
    /// </summary>
    public static EquatableArray<int> ComputeMemberIdMap(MockTypeModel standalone, MockTypeModel union)
    {
        var maxId = -1;
        foreach (var m in standalone.Methods)
        {
            if (m.MemberId > maxId) maxId = m.MemberId;
        }
        foreach (var p in standalone.Properties)
        {
            if (p.MemberId > maxId) maxId = p.MemberId;
            if (p.HasSetter && p.SetterMemberId > maxId) maxId = p.SetterMemberId;
        }

        if (maxId < 0)
        {
            return EquatableArray<int>.Empty;
        }

        var map = new int[maxId + 1];
        for (int i = 0; i < map.Length; i++)
        {
            map[i] = -1;
        }

        // First-wins lookups: union order is primary first, so deduplicated members resolve
        // to the primary's ID.
        var unionMethods = new Dictionary<string, MockMemberModel>();
        foreach (var m in union.Methods)
        {
            var key = GetMethodSignatureKey(m);
            if (!unionMethods.ContainsKey(key)) unionMethods.Add(key, m);
        }
        var unionProperties = new Dictionary<string, MockMemberModel>();
        foreach (var p in union.Properties)
        {
            var key = GetPropertySignatureKey(p);
            if (!unionProperties.ContainsKey(key)) unionProperties.Add(key, p);
        }

        foreach (var m in standalone.Methods)
        {
            if (unionMethods.TryGetValue(GetMethodSignatureKey(m), out var um))
            {
                map[m.MemberId] = um.MemberId;
            }
        }
        foreach (var p in standalone.Properties)
        {
            if (unionProperties.TryGetValue(GetPropertySignatureKey(p), out var up))
            {
                map[p.MemberId] = up.MemberId;
                if (p.HasSetter && up.HasSetter)
                {
                    map[p.SetterMemberId] = up.SetterMemberId;
                }
            }
        }

        return new EquatableArray<int>(ImmutableArray.Create(map));
    }

    /// <summary>
    /// Builds the pair model generating the setup/verify extension surface for one additional
    /// interface: primary identity (extensions target <c>Mock&lt;T1&gt;</c>), the interface's
    /// standalone members. Members the primary surface already exposes identically are excluded
    /// (their union slot IS the primary member — its extension covers both); members whose name
    /// clashes with a different primary member are renamed with a short interface prefix
    /// (<c>mock.IBar_Tag</c>) to keep call sites unambiguous. Returns null when nothing remains.
    /// </summary>
    public static MockTypeModel? BuildPairModel(MockTypeModel standalone, MockTypeModel primary, string interfaceFqn)
    {
        var shortName = MockImplBuilder.StripNamespaceFromFqn(interfaceFqn);

        // The primary's configurable surface, as the members builder filters it.
        var primaryMethods = primary.Methods
            .Where(m => m.ExplicitInterfaceName is null || m.IsStaticAbstract)
            .ToList();
        var primaryMethodSigs = new HashSet<string>(primaryMethods.Select(GetMethodSignatureKey));
        var primaryMethodNameParams = new HashSet<string>(primaryMethods.Select(GetMethodNameParamsKey));
        var primaryProperties = primary.Properties
            .Where(p => p.ExplicitInterfaceName is null || p.IsStaticAbstract)
            .ToList();
        var primaryPropertySigs = new HashSet<string>(primaryProperties.Select(GetPropertySignatureKey));
        var primaryNames = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var m in primaryMethods) primaryNames.Add(m.Name);
        foreach (var p in primaryProperties)
        {
            if (!p.IsIndexer) primaryNames.Add(p.Name);
        }
        foreach (var e in primary.Events) primaryNames.Add(e.Name);
        var primaryEventNames = new HashSet<string>(primary.Events.Select(e => e.Name), System.StringComparer.Ordinal);

        var methods = standalone.Methods
            .Where(m => !m.IsStaticAbstract)
            .Where(m => m.ExplicitInterfaceName is null) // interface-internal shims, same as the single-type surface
            .Where(m => !primaryMethodSigs.Contains(GetMethodSignatureKey(m)))
            .Select(m => primaryMethodNameParams.Contains(GetMethodNameParamsKey(m))
                ? m with { Name = $"{shortName}_{m.Name}" } // same name+params, different return — would be ambiguous
                : m)
            .ToImmutableArray();
        var properties = standalone.Properties
            .Where(p => !p.IsStaticAbstract)
            .Where(p => p.ExplicitInterfaceName is null)
            .Where(p => !primaryPropertySigs.Contains(GetPropertySignatureKey(p)))
            .Select(p => !p.IsIndexer && primaryNames.Contains(p.Name)
                ? p with { Name = $"{shortName}_{p.Name}" }
                : p)
            .ToImmutableArray();
        var events = standalone.Events
            .Where(e => !e.IsStaticAbstract && e.ExplicitInterfaceName is null)
            .Where(e => !primaryEventNames.Contains(e.Name)) // identical name = deduped onto the primary event
            .ToImmutableArray();

        if (methods.Length == 0 && properties.Length == 0 && events.Length == 0)
        {
            return null;
        }

        return standalone with
        {
            FullyQualifiedName = primary.FullyQualifiedName,
            OpenGenericTypeOfExpression = primary.OpenGenericTypeOfExpression,
            Name = primary.Name,
            Namespace = primary.Namespace,
            IsInterface = primary.IsInterface,
            IsAbstract = primary.IsAbstract,
            IsPartialMock = false,
            TypeParameters = EquatableArray<MockTypeParameterModel>.Empty,
            Methods = new EquatableArray<MockMemberModel>(methods),
            Properties = new EquatableArray<MockMemberModel>(properties),
            Events = new EquatableArray<MockEventModel>(events),
            AllInterfaces = primary.AllInterfaces,
            AdditionalInterfaceNames = new EquatableArray<string>(ImmutableArray.Create(interfaceFqn)),
            Constructors = EquatableArray<MockConstructorModel>.Empty,
            HasStaticAbstractMembers = false,
            IsPublic = primary.IsPublic && standalone.IsPublic,
            UseFallbackNamespace = primary.UseFallbackNamespace,
            IsSecondaryMemberSurface = true,
            SecondaryInterfaceFqn = interfaceFqn,
            SecondaryMemberIdMaps = EquatableArray<EquatableArray<int>>.Empty
        };
    }

    private static string GetMethodNameParamsKey(MockMemberModel m)
        => $"{m.Name}`{m.TypeParameters.Length}({string.Join(",", m.Parameters.Select(p => $"{p.Direction}:{p.FullyQualifiedType}"))})";

    private static string GetMethodSignatureKey(MockMemberModel m)
        => $"{GetMethodNameParamsKey(m)}:{m.ReturnType}";

    private static string GetPropertySignatureKey(MockMemberModel p)
        => p.IsIndexer
            ? $"this[{string.Join(",", p.Parameters.Select(q => q.FullyQualifiedType))}]:{p.ReturnType}"
            : $"{p.Name}:{p.ReturnType}";
}
