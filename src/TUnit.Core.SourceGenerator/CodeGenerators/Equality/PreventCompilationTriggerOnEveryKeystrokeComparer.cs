using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Equality;

/// <summary>
/// Treats two compilations as equal when they would produce the same reference-derived output:
/// same language, assembly name and metadata references. Syntax-only edits (ordinary keystrokes)
/// keep the same <see cref="MetadataReference"/> instances, so they compare equal and the
/// downstream reference walk is skipped. Adding, removing, rebuilding or editing a reference does not.
/// </summary>
public class PreventCompilationTriggerOnEveryKeystrokeComparer : IEqualityComparer<Compilation>
{
    public bool Equals(Compilation? x, Compilation? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null)
        {
            return false;
        }

        if (y is null)
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            return false;
        }

        return x.Language == y.Language
               && x.AssemblyName == y.AssemblyName
               && ReferencesEqual(x.ExternalReferences, y.ExternalReferences)
               && ReferencesEqual(x.DirectiveReferences, y.DirectiveReferences);
    }

    public int GetHashCode(Compilation obj)
    {
        unchecked
        {
            return (obj.Language.GetHashCode() * 397) ^ (obj.AssemblyName != null ? obj.AssemblyName.GetHashCode() : 0);
        }
    }

    private static bool ReferencesEqual(ImmutableArray<MetadataReference> x, ImmutableArray<MetadataReference> y)
    {
        // Syntax-only edits usually keep the same backing array, so this avoids the element walk.
        if (x == y)
        {
            return true;
        }

        if (x.Length != y.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Length; i++)
        {
            // Hosts reuse reference instances while the referenced file or project is unchanged.
            // A new instance means the reference was added, rebuilt or (for an IDE project
            // reference) its source was edited, which can change the types and dependencies the
            // reference walk selects. Rerun extraction then; AssemblyInfoModel equality keeps the
            // generated source cached when the extracted model is unchanged.
            if (!ReferenceEquals(x[i], y[i]))
            {
                return false;
            }
        }

        return true;
    }
}
