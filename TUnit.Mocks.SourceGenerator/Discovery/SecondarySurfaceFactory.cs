using System.Collections.Generic;
using System.Collections.Immutable;
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
///
/// Both consume a <see cref="SurfaceContext"/> built once per combo — the union lookups and the
/// primary surface index are invariant across the combo's additional interfaces.
/// </summary>
internal static class SecondarySurfaceFactory
{
    /// <summary>Per-combo invariants: union member lookups and the primary's configurable surface.</summary>
    internal sealed class SurfaceContext
    {
        /// <summary>Union members by full signature, first-wins — so members the union
        /// deduplicated against the primary resolve to the PRIMARY member's ID.</summary>
        public readonly Dictionary<string, MockMemberModel> UnionMethods = new(System.StringComparer.Ordinal);
        public readonly Dictionary<string, MockMemberModel> UnionProperties = new(System.StringComparer.Ordinal);

        public readonly HashSet<string> PrimaryMethodSignatures = new(System.StringComparer.Ordinal);
        public readonly HashSet<string> PrimaryMethodNameParams = new(System.StringComparer.Ordinal);
        public readonly HashSet<string> PrimaryPropertySignatures = new(System.StringComparer.Ordinal);
        public readonly HashSet<string> PrimaryNames = new(System.StringComparer.Ordinal);
        public readonly HashSet<string> PrimaryEventNames = new(System.StringComparer.Ordinal);
    }

    public static SurfaceContext CreateContext(MockTypeModel union, MockTypeModel primary)
    {
        var context = new SurfaceContext();

        foreach (var m in union.Methods)
        {
            var key = GetMethodSignatureKey(GetMethodNameParamsKey(m), m);
            if (!context.UnionMethods.ContainsKey(key)) context.UnionMethods.Add(key, m);
        }
        foreach (var p in union.Properties)
        {
            var key = GetPropertySignatureKey(p);
            if (!context.UnionProperties.ContainsKey(key)) context.UnionProperties.Add(key, p);
        }

        // The primary's configurable surface, as the members builder filters it.
        foreach (var m in primary.Methods)
        {
            if (m.ExplicitInterfaceName is not null && !m.IsStaticAbstract) continue;
            var nameParams = GetMethodNameParamsKey(m);
            context.PrimaryMethodNameParams.Add(nameParams);
            context.PrimaryMethodSignatures.Add(GetMethodSignatureKey(nameParams, m));
            context.PrimaryNames.Add(m.Name);
        }
        foreach (var p in primary.Properties)
        {
            if (p.ExplicitInterfaceName is not null && !p.IsStaticAbstract) continue;
            // Exclusion considers ALL primary properties (an identical-signature member is
            // union-deduped onto the primary slot whether or not it's configurable), but rename
            // decisions only consider names the builder actually exposes as extensions.
            context.PrimaryPropertySignatures.Add(GetPropertySignatureKey(p));
            if (p.IsConfigurableSurfaceProperty) context.PrimaryNames.Add(p.Name);
        }
        foreach (var e in primary.Events)
        {
            context.PrimaryNames.Add(e.Name);
            context.PrimaryEventNames.Add(e.Name);
        }

        return context;
    }

    /// <summary>
    /// Maps every member ID of <paramref name="standalone"/> (an additional interface's
    /// standalone model) to the matching member's ID in the combo's union model, correlating by
    /// full signature. Unmatched slots get -1. Members the union deduplicated against the primary
    /// map to the PRIMARY member's ID, so a setup made through either surface configures the same
    /// engine slot.
    /// </summary>
    public static EquatableArray<int> ComputeMemberIdMap(MockTypeModel standalone, SurfaceContext context)
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

        foreach (var m in standalone.Methods)
        {
            if (context.UnionMethods.TryGetValue(GetMethodSignatureKey(GetMethodNameParamsKey(m), m), out var um))
            {
                map[m.MemberId] = um.MemberId;
            }
        }
        foreach (var p in standalone.Properties)
        {
            if (context.UnionProperties.TryGetValue(GetPropertySignatureKey(p), out var up))
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
    public static MockTypeModel? BuildPairModel(
        MockTypeModel standalone, MockTypeModel primary, string interfaceFqn, SurfaceContext context)
    {
        var shortName = MockImplBuilder.StripNamespaceFromFqn(interfaceFqn);

        var methods = ImmutableArray.CreateBuilder<MockMemberModel>();
        foreach (var m in standalone.Methods)
        {
            // Static abstracts have no bridge on the multi path; explicit members are
            // interface-internal shims — both excluded, same as the single-type surface.
            if (m.IsStaticAbstract || m.ExplicitInterfaceName is not null) continue;
            var nameParams = GetMethodNameParamsKey(m);
            if (context.PrimaryMethodSignatures.Contains(GetMethodSignatureKey(nameParams, m))) continue;
            // Rename changes the extension's Name only; MemberId stays the standalone ordinal and
            // ComputeMemberIdMap correlates by the ORIGINAL signature key, so the map is unaffected.
            methods.Add(context.PrimaryMethodNameParams.Contains(nameParams)
                ? m with { Name = $"{shortName}_{m.Name}" } // same name+params, different return — would be ambiguous
                : m);
        }

        var properties = ImmutableArray.CreateBuilder<MockMemberModel>();
        foreach (var p in standalone.Properties)
        {
            if (p.IsStaticAbstract || p.ExplicitInterfaceName is not null) continue;
            if (context.PrimaryPropertySignatures.Contains(GetPropertySignatureKey(p))) continue;
            properties.Add(!p.IsIndexer && context.PrimaryNames.Contains(p.Name)
                ? p with { Name = $"{shortName}_{p.Name}" }
                : p);
        }

        var events = ImmutableArray.CreateBuilder<MockEventModel>();
        foreach (var e in standalone.Events)
        {
            if (e.IsStaticAbstract || e.ExplicitInterfaceName is not null) continue;
            if (context.PrimaryEventNames.Contains(e.Name)) continue; // identical name = deduped onto the primary event
            events.Add(e);
        }

        if (methods.Count == 0 && properties.Count == 0 && events.Count == 0)
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
            Methods = new EquatableArray<MockMemberModel>(methods.ToImmutable()),
            Properties = new EquatableArray<MockMemberModel>(properties.ToImmutable()),
            Events = new EquatableArray<MockEventModel>(events.ToImmutable()),
            AllInterfaces = primary.AllInterfaces,
            AdditionalInterfaceNames = new EquatableArray<string>(ImmutableArray.Create(interfaceFqn)),
            Constructors = EquatableArray<MockConstructorModel>.Empty,
            HasStaticAbstractMembers = false,
            IsPublic = primary.IsPublic && standalone.IsPublic,
            UseFallbackNamespace = primary.UseFallbackNamespace,
            IsSecondaryMemberSurface = true,
            SecondaryMemberIdMaps = EquatableArray<EquatableArray<int>>.Empty
        };
    }

    private static string GetMethodNameParamsKey(MockMemberModel m)
    {
        var sb = new System.Text.StringBuilder(m.Name).Append('`').Append(m.TypeParameters.Length).Append('(');
        for (int i = 0; i < m.Parameters.Length; i++)
        {
            if (i > 0) sb.Append(',');
            var p = m.Parameters[i];
            sb.Append(p.Direction).Append(':').Append(p.FullyQualifiedType);
        }
        return sb.Append(')').ToString();
    }

    private static string GetMethodSignatureKey(string nameParamsKey, MockMemberModel m)
        => $"{nameParamsKey}:{m.ReturnType}";

    private static string GetPropertySignatureKey(MockMemberModel p)
    {
        if (!p.IsIndexer)
        {
            return $"{p.Name}:{p.ReturnType}";
        }
        var sb = new System.Text.StringBuilder("this[");
        for (int i = 0; i < p.Parameters.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(p.Parameters[i].FullyQualifiedType);
        }
        return sb.Append("]:").Append(p.ReturnType).ToString();
    }
}
