using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace TUnit.Mocks.SourceGenerator.Extensions;

/// <summary>
/// Renames method type parameters whose names collide with a type parameter of the mocked type
/// (#7299). The generated mock call classes and extension blocks declare the mocked type's type
/// parameters and the method's side by side, so <c>IOutbox&lt;T&gt;</c> inheriting
/// <c>Invoke&lt;T&gt;()</c> would otherwise emit <c>MockCall&lt;T, T&gt;</c> (CS0692) and
/// <c>extension&lt;T&gt;(...) { Invoke&lt;T&gt;() }</c> (CS9289). Both parameters display as
/// <c>T</c>, so the rename is applied per symbol while a method's model strings are rendered:
/// every display helper routes through <see cref="ToDisplayString"/>, which swaps the renamed
/// type parameters' display parts while a <see cref="Scope"/> is active. Renames apply only while
/// the scope is active, so every model string for a method must be rendered eagerly inside it;
/// anything rendered later (for example by a deferred LINQ query) keeps the original names.
/// </summary>
internal static class MethodTypeParameterRenames
{
    [ThreadStatic]
    private static Dictionary<ITypeParameterSymbol, string>? _active;

    /// <summary>
    /// Activates renames for <paramref name="method"/>'s type parameters that collide with
    /// <paramref name="outerTypeParameterNames"/> until the returned scope is disposed.
    /// </summary>
    public static Scope Enter(IMethodSymbol method, IReadOnlyCollection<string>? outerTypeParameterNames)
    {
        var previous = _active;
        if (outerTypeParameterNames is null || outerTypeParameterNames.Count == 0 || method.TypeParameters.Length == 0)
        {
            return new Scope(previous, changed: false);
        }

        Dictionary<ITypeParameterSymbol, string>? renames = null;
        foreach (var typeParameter in method.TypeParameters)
        {
            if (!Contains(outerTypeParameterNames, typeParameter.Name))
            {
                continue;
            }

            renames ??= new Dictionary<ITypeParameterSymbol, string>(SymbolEqualityComparer.Default);
            renames[typeParameter] = CreateUniqueName(typeParameter.Name, method, outerTypeParameterNames, renames);
        }

        if (renames is null)
        {
            return new Scope(previous, changed: false);
        }

        _active = renames;
        return new Scope(previous, changed: true);
    }

    public static string GetName(ITypeParameterSymbol typeParameter)
        => _active is { } renames && renames.TryGetValue(typeParameter, out var renamed)
            ? renamed
            : typeParameter.Name;

    public static string ToDisplayString(ISymbol symbol, SymbolDisplayFormat format)
    {
        if (_active is not { } renames)
        {
            return symbol.ToDisplayString(format);
        }

        var parts = symbol.ToDisplayParts(format);
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Kind == SymbolDisplayPartKind.TypeParameterName
                && part.Symbol is ITypeParameterSymbol typeParameter
                && renames.TryGetValue(typeParameter, out var renamed))
            {
                builder.Append(renamed);
            }
            else
            {
                builder.Append(part.ToString());
            }
        }

        return builder.ToString();
    }

    private static string CreateUniqueName(
        string name,
        IMethodSymbol method,
        IReadOnlyCollection<string> outerTypeParameterNames,
        Dictionary<ITypeParameterSymbol, string> renames)
    {
        var candidate = name + "Method";
        for (var suffix = 2; IsTaken(candidate, method, outerTypeParameterNames, renames); suffix++)
        {
            candidate = name + "Method" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return candidate;
    }

    private static bool IsTaken(
        string candidate,
        IMethodSymbol method,
        IReadOnlyCollection<string> outerTypeParameterNames,
        Dictionary<ITypeParameterSymbol, string> renames)
    {
        // A method type parameter may not share the method's own name (CS0694).
        if (candidate == method.Name || Contains(outerTypeParameterNames, candidate))
        {
            return true;
        }

        foreach (var typeParameter in method.TypeParameters)
        {
            if (typeParameter.Name == candidate)
            {
                return true;
            }
        }

        // A method type parameter may not share a name with one of the method's value parameters (CS0412).
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Name == candidate)
            {
                return true;
            }
        }

        foreach (var renamed in renames.Values)
        {
            if (renamed == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Contains(IReadOnlyCollection<string> names, string name)
    {
        foreach (var existing in names)
        {
            if (existing == name)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Restores the previous renames on dispose. Scopes do not nest in practice, since each method
    /// model is rendered on its own, but restoring the previous state keeps nesting safe.
    /// </summary>
    public readonly struct Scope : IDisposable
    {
        private readonly Dictionary<ITypeParameterSymbol, string>? _previous;
        private readonly bool _changed;

        internal Scope(Dictionary<ITypeParameterSymbol, string>? previous, bool changed)
        {
            _previous = previous;
            _changed = changed;
        }

        public void Dispose()
        {
            if (_changed)
            {
                _active = _previous;
            }
        }
    }
}
