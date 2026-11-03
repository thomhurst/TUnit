using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Extensions;

/// <summary>
/// Qualifies types from extern-aliased references with their alias instead of <c>global::</c>
/// (#7300). A reference declared with <c>&lt;Aliases&gt;LibAlias&lt;/Aliases&gt;</c> is not part
/// of the <c>global</c> alias, so <c>global::Lib.IFoo</c> does not resolve (CS0234); the type is
/// only reachable as <c>LibAlias::Lib.IFoo</c>, in a file that declares <c>extern alias LibAlias;</c>.
/// <para>
/// Discovery transforms activate a <see cref="Scope"/> for their compilation, and
/// <see cref="MethodTypeParameterRenames.ToDisplayString"/> swaps the <c>global</c> keyword of each
/// aliased type while it is active. <see cref="AddExternAliasDirectives"/> then declares the aliases
/// each generated file uses. Compilations without aliased references pay one lookup per scope.
/// </para>
/// </summary>
internal static class ExternAliasQualification
{
    private static readonly ConditionalWeakTable<Compilation, AliasMap> Maps = new();
    private static readonly ConditionalWeakTable<Compilation, AliasMap>.CreateValueCallback CreateMap = Build;

    [ThreadStatic]
    private static AliasMap? _active;

    /// <summary>Whether a scope with at least one aliased reference is active.</summary>
    public static bool IsActive => _active is not null;

    /// <summary>
    /// Activates alias qualification for <paramref name="compilation"/>'s aliased references until the
    /// returned scope is disposed.
    /// </summary>
    public static Scope Enter(Compilation compilation)
    {
        var previous = _active;
        var map = Maps.GetValue(compilation, CreateMap);
        _active = map.Aliases.Length == 0 ? null : map;
        return new Scope(previous);
    }

    /// <summary>Every alias that qualifies a type of <paramref name="compilation"/>, ordinal-sorted.</summary>
    public static EquatableArray<string> GetAliases(Compilation compilation)
        => Maps.GetValue(compilation, CreateMap).Aliases;

    /// <summary>
    /// The alias that replaces <c>global</c> for <paramref name="type"/>, or <see langword="null"/>
    /// when its assembly is reachable through <c>global::</c> or no scope is active.
    /// </summary>
    public static string? GetAlias(INamedTypeSymbol type)
        => _active is { } map && type.ContainingAssembly is { } assembly && map.ByAssembly.TryGetValue(assembly, out var alias)
            ? alias
            : null;

    /// <summary>
    /// Inserts <c>extern alias</c> directives for the <paramref name="aliases"/> that
    /// <paramref name="source"/> qualifies a name with, after the leading comment and preprocessor
    /// lines (an extern alias must precede every using directive and namespace declaration).
    /// </summary>
    public static string AddExternAliasDirectives(string source, EquatableArray<string> aliases)
    {
        if (aliases.Length == 0)
        {
            return source;
        }

        StringBuilder? directives = null;
        foreach (var alias in aliases)
        {
            if (source.IndexOf(alias + "::", StringComparison.Ordinal) < 0)
            {
                continue;
            }

            directives ??= new StringBuilder();
            directives.Append("extern alias ").Append(alias).Append(';').Append('\n');
        }

        if (directives is null)
        {
            return source;
        }

        var insertAt = 0;
        while (insertAt < source.Length)
        {
            var lineEnd = source.IndexOf('\n', insertAt);
            var next = lineEnd < 0 ? source.Length : lineEnd + 1;
            var line = source.Substring(insertAt, next - insertAt).Trim();
            if (line.Length != 0 && !line.StartsWith("//", StringComparison.Ordinal) && !line.StartsWith("#", StringComparison.Ordinal))
            {
                break;
            }

            insertAt = next;
        }

        return source.Insert(insertAt, directives.Append('\n').ToString());
    }

    private static AliasMap Build(Compilation compilation)
    {
        // An alias can expose several assemblies. Prefer one owned only by the target
        // assembly so duplicate metadata names in other references do not become ambiguous.
        Dictionary<string, HashSet<IAssemblySymbol>>? aliasOwners = null;
        foreach (var reference in compilation.References)
        {
            if (reference.Properties.Aliases.IsDefaultOrEmpty ||
                compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
            {
                continue;
            }

            aliasOwners ??= new Dictionary<string, HashSet<IAssemblySymbol>>(StringComparer.Ordinal);
            foreach (var alias in reference.Properties.Aliases)
            {
                if (!aliasOwners.TryGetValue(alias, out var owners))
                {
                    owners = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
                    aliasOwners.Add(alias, owners);
                }

                owners.Add(assembly);
            }
        }

        Dictionary<IAssemblySymbol, string>? byAssembly = null;
        foreach (var reference in compilation.References)
        {
            var aliases = reference.Properties.Aliases;
            if (aliases.IsDefaultOrEmpty || aliases.Contains(MetadataReferenceProperties.GlobalAlias))
            {
                continue;
            }

            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
            {
                continue;
            }

            byAssembly ??= new Dictionary<IAssemblySymbol, string>(SymbolEqualityComparer.Default);
            var selected = aliases[0];
            foreach (var alias in aliases)
            {
                if (aliasOwners![alias].Count == 1)
                {
                    selected = alias;
                    break;
                }
            }

            // Store its C# spelling once for both qualified names and extern directives.
            byAssembly[assembly] = IdentifierEscaping.EscapeIdentifier(selected);
        }

        if (byAssembly is null)
        {
            return AliasMap.Empty;
        }

        var distinct = byAssembly.Values.Distinct(StringComparer.Ordinal).OrderBy(static alias => alias, StringComparer.Ordinal);
        return new AliasMap(byAssembly, new EquatableArray<string>(distinct.ToImmutableArray()));
    }

    internal sealed class AliasMap
    {
        public static readonly AliasMap Empty = new(
            new Dictionary<IAssemblySymbol, string>(SymbolEqualityComparer.Default),
            EquatableArray<string>.Empty);

        public AliasMap(Dictionary<IAssemblySymbol, string> byAssembly, EquatableArray<string> aliases)
        {
            ByAssembly = byAssembly;
            Aliases = aliases;
        }

        public Dictionary<IAssemblySymbol, string> ByAssembly { get; }

        public EquatableArray<string> Aliases { get; }
    }

    /// <summary>Restores the previously active aliases on dispose.</summary>
    public readonly struct Scope : IDisposable
    {
        private readonly AliasMap? _previous;

        internal Scope(AliasMap? previous) => _previous = previous;

        public void Dispose() => _active = _previous;
    }
}
